using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;
using Dazinator.AspNet.Extensions.FileProviders.Directory;
using Microsoft.Extensions.Primitives;
using System.Threading;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    public class InMemoryFileProvider : IFileProvider, IDisposable
    {

        private readonly ConcurrentDictionary<string, ChangeTokenInfo> _matchInfoCache = new ConcurrentDictionary<string, ChangeTokenInfo>();
        private readonly Lazy<DirectoryWatcher> _dirWatcher;

        public InMemoryFileProvider() : this(new InMemoryDirectory())
        {
        }

        public InMemoryFileProvider(IDirectory directory)
        {
            Directory = directory;
            _dirWatcher = new Lazy<DirectoryWatcher>(() =>
            {
                var watcher = new DirectoryWatcher(Directory);
                watcher.ItemAdded += Watcher_ItemAdded;
                watcher.ItemDeleted += Watcher_ItemDeleted;
                watcher.ItemUpdated += Watcher_ItemUpdated;
                return watcher;
            });
        }

        private void Watcher_ItemUpdated(object sender, DirectoryWatcherFilterMatchedEventArgs<DirectoryItemUpdatedEventArgs> e)
        {
            SignalTokens(e.MatchedFilters);
        }

        private void Watcher_ItemDeleted(object sender, DirectoryWatcherFilterMatchedEventArgs<DirectoryItemDeletedEventArgs> e)
        {
            SignalTokens(e.MatchedFilters);
        }

        private void Watcher_ItemAdded(object sender, DirectoryWatcherFilterMatchedEventArgs<DirectoryItemAddedEventArgs> e)
        {
            SignalTokens(e.MatchedFilters);
        }
       
        private void SignalTokens(string[] keys)
        {
            
            foreach (var key in keys)
            {
                ChangeTokenInfo changeToken = null;
                if (_matchInfoCache.TryRemove(key, out changeToken))
                {
                    changeToken.TokenSource.Cancel();
                }

                //if (_matchInfoCache.TryGetValue(filter, out changeToken))
                //{
                //    // return existing token for this file path.
                //   // yield return changeToken;
                //    //var inMemoryChangeToken = changeToken as InMemoryChangeToken;
                //    //if (inMemoryChangeToken != null)
                //    //{
                //    //    yield return inMemoryChangeToken;
                //    //}
                //}
            }
        }

        public DirectoryWatcher DirectoryWatcher
        {
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

           // var subPath = SubPathInfo.Parse(filter);
            // var subPathString = subPath.ToString();
            // IChangeToken existing;
            var resultToken = GetOrAddChangeToken(filter, (t) =>
            {
                DirectoryWatcher.AddFilter(filter);
            });
            return resultToken;
        }

        private IChangeToken GetOrAddChangeToken(string key, Action<ChangeTokenInfo> onNewTokenAdded)
        {
            ChangeTokenInfo fileToken;
            if (_matchInfoCache.TryGetValue(key, out fileToken))
            {
                // return existing token for this file path.
            }
            else
            {
                // var newToken = new InMemoryChangeToken();
                // var newToken = tokenFactory();

                var cancellationTokenSource = new CancellationTokenSource();
                var cancellationChangeToken = new CancellationChangeToken(cancellationTokenSource.Token);
                var tokenInfo = new ChangeTokenInfo(cancellationChangeToken, cancellationTokenSource);
                // tokenInfo = _wildcardTokenLookup.GetOrAdd(pattern, tokenInfo);
                fileToken = _matchInfoCache.GetOrAdd(key, tokenInfo);
                onNewTokenAdded(fileToken);
            }
            return fileToken.ChangeToken;
        }

        /// <summary>
        /// Disposes the provider. Change tokens may not trigger after the provider is disposed.
        /// </summary>
        public void Dispose()
        {
            if (_dirWatcher.IsValueCreated)
            {
                _dirWatcher.Value.ItemAdded -= Watcher_ItemAdded;
                _dirWatcher.Value.ItemDeleted -=Watcher_ItemDeleted;
                _dirWatcher.Value.ItemUpdated -= Watcher_ItemUpdated;
                _dirWatcher.Value.Dispose();
            }

        }

        private class ChangeTokenInfo
        {
            public ChangeTokenInfo(IChangeToken changeToken, CancellationTokenSource tokenSource)
            {
                ChangeToken = changeToken;
                TokenSource = tokenSource;
            }

            public IChangeToken ChangeToken { get; }

            public CancellationTokenSource TokenSource { get; }
        }
    }
}
