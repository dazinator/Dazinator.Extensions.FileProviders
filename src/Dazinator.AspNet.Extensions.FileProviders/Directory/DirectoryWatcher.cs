using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{


    public enum VisitMode
    {
        Register,
        Unregister       
    }

    public class DirectoryWatcher : BaseDirectoryVisitor
    {
        // private readonly IFolderDirectoryItem _folderItem;
        // private readonly bool _watchNewSubFolders;

        public event EventHandler<DirectoryItemUpdatedEventArgs> ItemUpdated;
        public event EventHandler<DirectoryItemAddedEventArgs> ItemAdded;
        public event EventHandler<DirectoryItemDeletedEventArgs> ItemDeleted;

        //private readonly bool _autoWatchNewSubFolders;
        private ConcurrentDictionary<string, IDirectoryItem> _watchingFolders;
        private List<string> _Filters;
        private VisitMode _visitMode;
        private bool _UnregisterWasSuccessful;

        private IDirectory _directory;

        // private string _pattern;

        /// <summary>
        /// Constructs a new instance of a directory watcher that can be used to subscribe to events that signal when changes are made to a directory.
        /// </summary>
        /// <param name="autoWatchNewSubFolders">If true, will automatically Watch any new subfolders that happen to be created in any directory currently being watched.</param>
        public DirectoryWatcher(IDirectory directory)
        {
           // _autoWatchNewSubFolders = autoWatchNewSubFolders;
            _watchingFolders = new ConcurrentDictionary<string, IDirectoryItem>();
            _directory = directory;
            _Filters = new List<string>();
            _visitMode = VisitMode.Register;
            _directory.Accept(this); // visit all items in the directory and attach handlers for event notifications.
            // _pattern = pattern;
        }


        public override void Visit(FileDirectoryItem item)
        {
            switch (_visitMode)
            {
                case VisitMode.Register:
                    Register(item);
                    break;
                case VisitMode.Unregister:
                    _UnregisterWasSuccessful = false;
                    _UnregisterWasSuccessful = Unregister(item);
                    break;               
            }
        }

        public override void Visit(FolderDirectoryItem item)
        {
            switch (_visitMode)
            {
                case VisitMode.Register:
                    Register(item);
                    foreach (var child in item)
                    {
                        child.Accept(this);
                    }
                    break;
                case VisitMode.Unregister:
                    _UnregisterWasSuccessful = false;
                    _UnregisterWasSuccessful = Unregister(item);
                    break;               
            }

        }


        /// <summary>
        /// Watches a particular folder in the directory, and raises an event whenever the folder has a new item added to it, or an item deleted from it, or an item 
        /// </summary>
        /// <param name="folder"></param>
        private void Register(IFolderDirectoryItem folder)
        {
            _watchingFolders.AddOrUpdate(folder.Path, (key) =>
            {
                // a folder can be watched for items that are added to it,
                // or for the folder being deleted.
                // watching for an "update" to a folder is basically the folder being renamed.

                folder.ItemAdded += Folder_ItemAdded; // item added to the folder.
                folder.Deleted += Item_Deleted; // the folder was deleted.
                folder.Updated += Item_Updated; // folder was renamed.
                return folder;
            }, (key, item) => item);
        }

        /// <summary>
        /// Watches a specific file in the directory, and raises an event when the file is updated, or deleted.
        /// </summary>
        /// <param name="file"></param>
        private void Register(IFileDirectoryItem file)
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

        private bool Unregister(IFileDirectoryItem file)
        {
            IDirectoryItem removed;
            if (_watchingFolders.TryRemove(file.Path, out removed))
            {
                removed.Deleted -= Item_Deleted;
                removed.Updated -= Item_Updated;
                return true;
            }

            return false;
        }

        private bool Unregister(IFolderDirectoryItem folder)
        {
            // unwire events, as the folder has been deleted we no longer need to listen to it for changes.

            IDirectoryItem removed;
            if (_watchingFolders.TryRemove(folder.Path, out removed))
            {
                var removedFolder = removed as IFolderDirectoryItem;
                if (removedFolder != null)
                {
                    removedFolder.ItemAdded -= Folder_ItemAdded;
                    removedFolder.Updated -= Item_Updated;
                    removedFolder.Deleted -= Item_Deleted;
                }
                else
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Unregisters all directories and files, unregistering event handlers.
        /// </summary>
        /// <returns></returns>
        private bool UnregisterAll()
        {
            List<string> invalidKeys = new List<string>();
            var keys = _watchingFolders.Keys.ToArray();

            var currentMode = _visitMode;
            _visitMode = VisitMode.Unregister;

            foreach (var key in keys)
            {
                IDirectoryItem watchedFolder;
                if (_watchingFolders.TryGetValue(key, out watchedFolder))
                {
                    _UnregisterWasSuccessful = false;
                    watchedFolder.Accept(this);
                    if (!_UnregisterWasSuccessful)
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

            _visitMode = currentMode;

            if (invalidKeys.Any())
            {
                return false;
            }

            return true;

        }

        //private void Folder_Updated(object sender, DirectoryItemUpdatedEventArgs e)
        //{
        //    // if the folder matches 
        //    OnRaiseItemUpdated(e);
        //}

        /// <summary>
        /// Handles an item being added to a folder that is being watched.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Folder_ItemAdded(object sender, DirectoryItemAddedEventArgs e)
        {
            // register the new item.
            var currentMode = _visitMode;
            _visitMode = VisitMode.Register;
            e.NewItem.Accept(this);
            _visitMode = currentMode;
            OnRaiseItemAdded(e);
        }

        /// <summary>
        /// handles an item being deleted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_Deleted(object sender, DirectoryItemDeletedEventArgs e)
        {
            // unregister the item.
            var currentMode = _visitMode;
            _visitMode = VisitMode.Unregister;
            e.DeletedItem.Accept(this);
            _visitMode = currentMode;
            OnRaiseItemDeleted(e);
           
        }

        /// <summary>
        /// Handles an item being updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_Updated(object sender, DirectoryItemUpdatedEventArgs e)
        {
            // either file change, or folder rename.
            // no need to register / unregister. 
            OnRaiseItemUpdated(e);
        }

        protected virtual void OnRaiseItemAdded(DirectoryItemAddedEventArgs args)
        {
            // TODO: Only raise the event if the item/s matches watch pattern.

           
            EventHandler<DirectoryItemAddedEventArgs> handler = ItemAdded;
            handler?.Invoke(this, args);
        }
        protected virtual void OnRaiseItemUpdated(DirectoryItemUpdatedEventArgs args)
        {
            // TODO: Only raise the event if the item/s matches watch pattern.
           
            EventHandler<DirectoryItemUpdatedEventArgs> handler = ItemUpdated;
            handler?.Invoke(this, args);

            ItemUpdated?.Invoke(this, args);
        }
        protected virtual void OnRaiseItemDeleted(DirectoryItemDeletedEventArgs args)
        {
            // TODO: Only raise the event if the item/s matches watch pattern.

            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<DirectoryItemDeletedEventArgs> handler = ItemDeleted;
            ItemDeleted?.Invoke(this, args);
        }
      
        /// <summary>
        /// Adds a filter so that only if an item in the directory has a path that matches the filter, will an event be raised when it is modified.
        /// </summary>
        /// <param name="pattern"></param>
        public void AddFilter(string pattern)
        {
            _Filters.Add(pattern);

        }

    }
}