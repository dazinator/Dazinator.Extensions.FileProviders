using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public class FolderDirectoryItem : IFolderDirectoryItem
    {
        public FolderDirectoryItem(string name, IFolderDirectoryItem parentFolder) : this(new DirectoryFileInfo(name), parentFolder)
        {

        }

        public FolderDirectoryItem(IFileInfo directoryFileInfo, IFolderDirectoryItem parentFolder)
        {
            FileInfo = directoryFileInfo;
            ParentFolder = parentFolder;
            Items = new Dictionary<string, IDirectoryItem>();
        }

        #region IFolderDirectoryItem
        public IFolderDirectoryItem ParentFolder { get; set; }

        public Dictionary<string, IDirectoryItem> Items { get; set; }

        public string Path => ParentFolder?.Path + "/" + Name;

        public string Name => FileInfo.Name;

        public bool IsFolder => true;

        public IFileInfo FileInfo { get; private set; }

        /// <summary>
        /// Navigates to a folder within the current directory, and creates the folder if it doesn't already exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IFolderDirectoryItem GetOrAddFolder(string name)
        {
            return GetOrAddItem(name, (folderName) => new FolderDirectoryItem(folderName, this)) as IFolderDirectoryItem;
        }

        /// <summary>
        ///Adds ther file to the folder, overwriting the file if it exists..
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public IFileDirectoryItem AddFile(IFileInfo file)
        {
            var name = file.Name;
            var newItem = new FileDirectoryItem(file, this);
            AddItem(name, newItem);
            return newItem;
        }

        /// <summary>
        ///Adds ther file to the folder, overwriting the file if it exists..
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public IFileDirectoryItem UpdateFile(IFileInfo file)
        {
            var existingItem = GetFileItem(file.Name);
            existingItem.Update(file);
            return existingItem;
        }

        private IFileDirectoryItem GetFileItem(string path)
        {
            var existingItem = GetChildDirectoryItem(path) as IFileDirectoryItem;
            if (existingItem == null)
            {
                throw new InvalidOperationException("No such file exists.");
            }
            return existingItem;
        }

        /// <summary>
        /// returns the item in the folder ith the specified name, or null if it doesn't exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The item in the folder, or nulll if it doesn't exist.</returns>
        public IDirectoryItem GetChildDirectoryItem(string name)
        {
            if (Items.ContainsKey(name))
            {
                var existing = Items[name];
                return existing;
            }

            return null;

        }
       
        public event EventHandler<DirectoryItemAddedEventArgs> ItemAdded;
        public event EventHandler<DirectoryItemUpdatedEventArgs> Updated;
        public event EventHandler<DirectoryItemDeletedEventArgs> Deleted;

        // public event EventHandler<DirectoryItemDeletedEventArgs> ItemDeleted;

        #endregion

        #region IEnumerable

        public IEnumerator<IDirectoryItem> GetEnumerator()
        {
            return Items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.Values.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Returns the existing item from the current folder, based on its name, and if the item doesn't
        /// exist, executes a callback to create the item, and adds it to the folder.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="createItemCallback"></param>
        /// <returns></returns>
        private IDirectoryItem GetOrAddItem(string name, Func<string, IDirectoryItem> createItemCallback)
        {
            if (Items.ContainsKey(name))
            {
                var existing = Items[name];
                return existing;
            }

            var newItem = createItemCallback(name);
            Items.Add(name, newItem);
            OnRaiseItemAdded(newItem);
            return newItem;
        }

        ///// <summary>
        ///// Updates an existing item in the folder.
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="updatedItem"></param>
        ///// <returns></returns>
        //private IDirectoryItem UpdateItem(string name, IDirectoryItem updatedItem)
        //{
        //    var existingItem = GetChildDirectoryItem(name);
        //    if (existingItem == null)
        //    {
        //        throw new InvalidOperationException("No such item exists.");
        //    }

        //    if (!existingItem.IsFolder)
        //    {
        //        var file = existingItem as IFile
        //    }

        //    Items[name] = updatedItem;
        //    OnRaiseItemUpdated(existingItem, updatedItem);
        //    return updatedItem;
        //}

        /// <summary>
        /// Adds an item to folder directory.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private IDirectoryItem AddItem(string name, IDirectoryItem newItem)
        {
            if (Items.ContainsKey(name))
            {
                throw new InvalidOperationException("Cannot add item to the directory, as an item with the same name already exists.");

            }

            Items.Add(name, newItem);
            OnRaiseItemAdded(newItem);
            return newItem;
        }

        /// <summary>
        /// Removes an item from the folder directory.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool RemoveItem(string name)
        {
            var existing = Items[name];
            var result = Items.Remove(name);
            if (result)
            {
                existing.OnDeleted();
            }
            return result;
        }

        protected virtual void OnRaiseItemAdded(IDirectoryItem newItem)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<DirectoryItemAddedEventArgs> handler = ItemAdded;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                var args = new DirectoryItemAddedEventArgs(newItem);

                // Use the () operator to raise the event.
                handler(this, args);
            }
        }
      
        public void OnDeleted()
        {
            FileInfo = new NotFoundFileInfo(this.FileInfo.Name);
            OnRaiseItemDeleted();
        }

        protected virtual void OnRaiseItemDeleted()
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<DirectoryItemDeletedEventArgs> handler = Deleted;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                var args = new DirectoryItemDeletedEventArgs(this);

                // Use the () operator to raise the event.
                handler(this, args);
            }
        }

        public override int GetHashCode()
        {
            return this.Path.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var item = obj as IDirectoryItem;

            if (item == null)
            {
                return false;
            }

            return this.Path.Equals(item.Path);
        }



        //protected virtual void OnRaiseItemUpdated(IDirectoryItem oldItem, IDirectoryItem newItem)
        //{
        //    // Make a temporary copy of the event to avoid possibility of
        //    // a race condition if the last subscriber unsubscribes
        //    // immediately after the null check and before the event is raised.
        //    EventHandler<DirectoryItemUpdatedEventArgs> handler = ItemUpdated;

        //    // Event will be null if there are no subscribers
        //    if (handler != null)
        //    {
        //        var args = new DirectoryItemUpdatedEventArgs(oldItem, newItem);

        //        // Use the () operator to raise the event.
        //        handler(this, args);
        //    }
        //}

    }
}
