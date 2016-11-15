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
    public class InMemoryFileProvider : IFileProvider
    {

        private readonly ConcurrentDictionary<string, IChangeToken> _matchInfoCache = new ConcurrentDictionary<string, IChangeToken>();
        private readonly DirectoryWatcher _dirWatcher;

        public InMemoryFileProvider(IDirectory directory)
        {
            Directory = directory;
            _dirWatcher = new DirectoryWatcher(directory);
            _dirWatcher.ItemUpdated += DirectoryWatcher_ItemUpdated;
            _dirWatcher.ItemDeleted += DirectoryWatcher_ItemDeleted;
            _dirWatcher.ItemAdded += DirWatcher_ItemAddedToDirectory;
        }

        public IDirectory Directory { get; set; }

        public IFileInfo GetFileInfo(string subpath)
        {
            IFileDirectoryItem result = Directory.GetFile(subpath);
            if (result==null)
            {
                return new NotFoundFileInfo(subpath);
            }
            return result.FileInfo;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            IFolderDirectoryItem folder = Directory.GetFolder(subpath);
            if (folder==null)
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
            IChangeToken existing;

            // If we already have a changetoken for this filter, return the existing one.
            if (this._matchInfoCache.TryGetValue(subPath.ToString(), out existing))
            {
                return existing;
            }


            var fileTokens = new List<IChangeToken>();

            // We can perform a little optimisation.. 
            // We can examine the filter to determine if its a "pattern" or a "directory".
            // If it is, then it means it can match multiple files, so we will enumerate all search results.
            // if it isn#t it means it is specifying a single file, so we can break after finding the first matching file. 
            bool isComposite = subPath.IsPattern || string.IsNullOrWhiteSpace(subPath.Name);


            _dirWatcher.AddFilter(filter); // now only changes to item in the directory that match the filter will cause events to be raised / us to be notified.

            var resultToken = GetOrAddChangeToken(subPath.ToString(), () => new InMemoryChangeToken());

            // var results = Directory.Search(filter);

            IChangeToken resultToken = null;

            //foreach (var item in results)
            //{
               
            //    fileTokens.Add(resultToken);

            //    // watch this directory item (could be a folder or file) for changes.
            //    // and then when they occur, can signal the changetokens for them.
            //    _dirWatcher.Watch(item);

               

            //    if (!isComposite)
            //    {
            //        // stop enumerating if single file.
            //        break;
            //    }
            }

            //if (!fileTokens.Any())
            //{
            //    // no files matched, return null change token.
            //    resultToken = NullChangeToken.Singleton;
            //}

            if (fileTokens.Count > 1)
            {
                // many individual items in the directory were matched,
                // return a composite change token that wraps all of the individual change tokens.
                resultToken = new CompositeChangeToken(fileTokens.AsEnumerable<IChangeToken>().ToList());
               
            }

            //  matched one item in the directory, return it's single change token.
            GetOrAddChangeToken(subPath.ToString(), () => resultToken);
            return resultToken;

        }

      
        private void DirWatcher_ItemAddedToDirectory(object sender, DirectoryItemAddedEventArgs e)
        {
            // A directory we were watching has had an item added to it.
            // do we need to signal the change token?
            //throw new NotImplementedException();
            // get the change token for the directory.

            var key = e.NewItem.ParentFolder.Path;
            IChangeToken changeToken;
            if (_matchInfoCache.TryGetValue(key, out changeToken))
            {
                // return existing token for this file path.
                var inMemoryChangeToken = changeToken as InMemoryChangeToken;
                if (inMemoryChangeToken != null)
                {
                    // return existing token for this file path.
                    inMemoryChangeToken.HasChanged = true;
                    // TODO: not sure if this is correct to send IFileItem representing the new item, or the actual folder that
                    // item has been added to.
                    inMemoryChangeToken.RaiseCallback(e.NewItem.FileInfo);
                }
            }

          
        }

        private void DirectoryWatcher_ItemDeleted(object sender, DirectoryItemDeletedEventArgs e)
        {
            // an item we were watching has been deleted..  
            // do we need to signal the change token?
            var key = e.DeletedItem.Path;
            IChangeToken fileToken;
            if (_matchInfoCache.TryGetValue(key, out fileToken))
            {
                // return existing token for this file path.
                var inMemory = fileToken as InMemoryChangeToken;
                if (inMemory != null)
                {
                    // return existing token for this file path.
                    inMemory.HasChanged = true;
                    inMemory.RaiseCallback(e.DeletedItem.FileInfo);
                }
            }
        }

        private void DirectoryWatcher_ItemUpdated(object sender, DirectoryItemUpdatedEventArgs e)
        {
            // an item we were watching has been updated..  
            // do we need to signal the change token?

            var key = e.NewItem.Path;
            IChangeToken fileToken;
            if (_matchInfoCache.TryGetValue(key, out fileToken))
            {
                // return existing token for this file path.
                var inMemory = fileToken as InMemoryChangeToken;
                if (inMemory != null)
                {
                    // return existing token for this file path.
                    inMemory.HasChanged = true;
                    inMemory.RaiseCallback(e.NewItem.FileInfo);
                }
            }


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


    }
}
