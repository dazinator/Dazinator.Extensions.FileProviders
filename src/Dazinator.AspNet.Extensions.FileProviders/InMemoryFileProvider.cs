using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Linq;
using Dazinator.AspNet.Extensions.FileProviders.Directory;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    public class InMemoryFileProvider : IFileProvider, IDisposable
    {

        private readonly ConcurrentDictionary<string, IChangeToken> _matchInfoCache = new ConcurrentDictionary<string, IChangeToken>();
        private readonly Lazy<DirectoryWatcher> _dirWatcher;

        public InMemoryFileProvider() : this(new InMemoryDirectory())
        {
        }

        public InMemoryFileProvider(IDirectory directory)
        {
            Directory = directory;
            _dirWatcher = new Lazy<DirectoryWatcher>(() =>
            {
                return new DirectoryWatcher(Directory);
            });
        }

        private void _dirWatcher_ItemUpdated(object sender, DirectoryWatcherFilterMatchedEventArgs<DirectoryItemUpdatedEventArgs> e)
        {
            InMemoryChangeToken[] tokens = GetTokens(e.MatchedFilters).ToArray();
            foreach (var token in tokens)
            {
                token.HasChanged = true;
                // send the old or the new item? guessing send the new!
                token.RaiseCallback(e.DirectoryItemEventArgs.NewItem.FileInfo);
            }
        }

        private void _dirWatcher_ItemDeleted(object sender, DirectoryWatcherFilterMatchedEventArgs<DirectoryItemDeletedEventArgs> e)
        {
            // find the tokens
            InMemoryChangeToken[] tokens = GetTokens(e.MatchedFilters).ToArray();
            foreach (var token in tokens)
            {
                token.HasChanged = true;
                token.RaiseCallback(e.DirectoryItemEventArgs.DeletedItem.FileInfo);
            }
        }

        private void _dirWatcher_ItemAdded(object sender, DirectoryWatcherFilterMatchedEventArgs<DirectoryItemAddedEventArgs> e)
        {
            // A directory we were watching has had an item added to it.
            //    // do we need to signal the change token?          

            // find the tokens
            InMemoryChangeToken[] tokens = GetTokens(e.MatchedFilters).ToArray();
            foreach (var token in tokens)
            {
                token.HasChanged = true;
                token.RaiseCallback(e.DirectoryItemEventArgs.NewItem.FileInfo);
            }

        }

        private IEnumerable<InMemoryChangeToken> GetTokens(string[] matchedFilters)
        {
            IChangeToken changeToken;
            foreach (var filter in matchedFilters)
            {

                if (_matchInfoCache.TryGetValue(filter, out changeToken))
                {
                    // return existing token for this file path.
                    var inMemoryChangeToken = changeToken as InMemoryChangeToken;
                    if (inMemoryChangeToken != null)
                    {
                        yield return inMemoryChangeToken;
                    }
                }
            }
        }

        public DirectoryWatcher DirectoryWatcher {
            get
            {
                // DirectoryWatcher is created on first access here.
                return _dirWatcher.Value;
            }
        }

        public IDirectory Directory { get; set; }

        public IFileInfo GetFileInfo(string subpath)
        {
            IFileDirectoryItem result = Directory.GetFile(subpath);
            if (result == null)
            {
                return new NotFoundFileInfo(subpath);
            }
            return result.FileInfo;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            IFolderDirectoryItem folder = Directory.GetFolder(subpath);
            if (folder == null)
            {
                return new NotFoundDirectoryContents();
            }

            return new EnumerableFolderDirectoryContents(folder);

        }

        public IChangeToken Watch(string filter)
        {
            if (filter == null)
            {
                return NullChangeToken.Singleton;
            }

            var subPath = SubPathInfo.Parse(filter);
            var subPathString = subPath.ToString();
            IChangeToken existing;
            // If we already have a changetoken for this filter, return the existing one.
            if (this._matchInfoCache.TryGetValue(subPathString, out existing))
            {
                return existing;
            }

            DirectoryWatcher.AddFilter(subPathString); // now only changes to item in the directory that match the filter will cause events to be raised / us to be notified.

            var resultToken = GetOrAddChangeToken(subPathString, () => new InMemoryChangeToken());
            return resultToken;

        }

        private IChangeToken GetOrAddChangeToken(string key, Func<IChangeToken> tokenFactory)
        {
            IChangeToken fileToken;
            if (_matchInfoCache.TryGetValue(key, out fileToken))
            {
                // return existing token for this file path.
            }
            else
            {
                // var newToken = new InMemoryChangeToken();
                var newToken = tokenFactory();
                fileToken = _matchInfoCache.GetOrAdd(key, newToken);
            }
            return fileToken;
        }

        /// <summary>
        /// Disposes the provider. Change tokens may not trigger after the provider is disposed.
        /// </summary>
        public void Dispose()
        {
            if (_dirWatcher.IsValueCreated)
            {
                _dirWatcher.Value.Dispose();
            }
           
        }
    }
}
