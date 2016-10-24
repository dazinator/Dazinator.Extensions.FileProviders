using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public class DirectoryWatcher
    {
        // private readonly IFolderDirectoryItem _folderItem;
        // private readonly bool _watchNewSubFolders;

        public event EventHandler<DirectoryItemUpdatedEventArgs> ItemUpdated;
        public event EventHandler<DirectoryItemAddedEventArgs> ItemAdded;
        public event EventHandler<DirectoryItemDeletedEventArgs> ItemDeleted;

        private readonly bool _autoWatchNewSubFolders;
        private ConcurrentDictionary<string, IDirectoryItem> _watchingFolders;

        /// <summary>
        /// Constructs a new instance of a directory watcher that can be used to subscribe to events that signal when changes are made to a directory.
        /// </summary>
        /// <param name="autoWatchNewSubFolders">If true, will automatically Watch any new subfolders that happen to be created in any directory currently being watched.</param>
        public DirectoryWatcher(bool autoWatchNewSubFolders = false)
        {
            _autoWatchNewSubFolders = autoWatchNewSubFolders;
            _watchingFolders = new ConcurrentDictionary<string, IDirectoryItem>();
        }


        public void Watch(IDirectoryItem item)
        {
            if (item.IsFolder)
            {
                var folder = item as IFolderDirectoryItem;
                Watch(folder);
            }

            var file = item as IFileDirectoryItem;
            Watch(file);

        }

        /// <summary>
        /// Watches a particular folder in the directory, and raises an event whenever the folder has a new item added to it, or an item deleted from it, or an item 
        /// </summary>
        /// <param name="folder"></param>
        public void Watch(IFolderDirectoryItem folder)
        {
            _watchingFolders.AddOrUpdate(folder.Path, (key) =>
            {
                // a folder can be watched for items that are added to it,
                // or for the folder being deleted.
                // watching for an "update" to a folder isn't handled.

                folder.ItemAdded += Folder_ItemAdded; // item added to the folder.
                folder.Deleted += Item_Deleted; // the folder was deleted.
                //folder.ItemUpdated += Folder_ItemUpdated;
                return folder;
            }, (key, item) => item);
        }

        /// <summary>
        /// Watches a specific file in the directory, and raises an event when the file is updated, or deleted.
        /// </summary>
        /// <param name="file"></param>
        public void Watch(IFileDirectoryItem file)
        {
            // An existing file can be watched for deltion or modification (updates). 
            // Creation is an event that is raised at the folder level for new child item.
            _watchingFolders.AddOrUpdate(file.Path, (key) =>
            {
                // folder.ItemAdded += Folder_ItemAdded;
                file.Deleted += Item_Deleted; // the file was deleted.
                file.Updated += Item_Updated; // the file was updated.
                return file;
            }, (key, item) => item);
        }

        public bool StopWatching(IDirectoryItem folder)
        {
            // unwire events, as the folder has been deleted we no longer need to listen to it for changes.
            IDirectoryItem removed;
            if (_watchingFolders.TryRemove(folder.Path, out removed))
            {
                if (removed.IsFolder)
                {
                    var removedFolder = removed as IFolderDirectoryItem;
                    if (removedFolder != null)
                    {
                        removedFolder.ItemAdded -= Folder_ItemAdded;
                    }
                    else
                    {
                        return false;
                    }
                }

                removed.Deleted -= Item_Deleted;
                removed.Updated -= Item_Updated;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Stops watching all folders currently being watched.
        /// </summary>
        /// <returns></returns>
        public bool StopWatching()
        {

            List<string> invalidKeys = new List<string>();

            var keys = _watchingFolders.Keys.ToArray();
            foreach (var key in keys)
            {
                IDirectoryItem watchedFolder;
                if (_watchingFolders.TryGetValue(key, out watchedFolder))
                {
                    if (!StopWatching(watchedFolder))
                    {
                        // can't stop watching this folder, perhaps it was removed by something else?
                        invalidKeys.Add(key);
                        continue;
                    }
                }
                else
                {
                    // can't get this value, perhaps another thread has removed it?
                    invalidKeys.Add(key);
                    continue;
                }

            }

            if (invalidKeys.Any())
            {
                return false;
            }

            return true;

        }

        /// <summary>
        /// Handles an item being added to a folder that is being watched.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Folder_ItemAdded(object sender, DirectoryItemAddedEventArgs e)
        {
            if (e.NewItem.IsFolder)
            {
                // register for changes to this folder.
                var newFolder = e.NewItem as IFolderDirectoryItem;
                if (newFolder != null)
                {
                    // wire up events so we can detect items being added and removed from this new folder.
                    if (_autoWatchNewSubFolders)
                    {
                        Watch(newFolder);
                        //  newFolder.ItemAdded += Folder_ItemAdded;
                        // newFolder.ItemDeleted += Folder_ItemDeleted;
                    }
                }
               
            }

            OnRaiseItemAdded(e);
        }

        /// <summary>
        /// Handles an item being updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_Updated(object sender, DirectoryItemUpdatedEventArgs e)
        {
            OnRaiseItemUpdated(e);
        }

        /// <summary>
        /// handles an item being deleted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_Deleted(object sender, DirectoryItemDeletedEventArgs e)
        {

            if (e.DeletedItem.IsFolder)
            {
                // de-register for changes to this folder.
                var oldFolder = e.DeletedItem as IFolderDirectoryItem;
                if (oldFolder != null)
                {
                    //if (_autoWatchNewSubFolders)
                    //{
                    // ensure we automatically stop watching on any folder that gets deleted.
                    StopWatching(oldFolder);
                    //}
                }
            }

            OnRaiseItemDeleted(e);
        }

      
        protected virtual void OnRaiseItemAdded(DirectoryItemAddedEventArgs args)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<DirectoryItemAddedEventArgs> handler = ItemAdded;
            handler?.Invoke(this, args);
        }
        protected virtual void OnRaiseItemUpdated(DirectoryItemUpdatedEventArgs args)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            ItemUpdated?.Invoke(this, args);
        }
        protected virtual void OnRaiseItemDeleted(DirectoryItemDeletedEventArgs args)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            ItemDeleted?.Invoke(this, args);
        }

    }
}