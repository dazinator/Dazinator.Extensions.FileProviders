using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public class FolderDirectoryItem : BaseDirectoryItem, IFolderDirectoryItem
    {

        public event EventHandler<DirectoryItemAddedEventArgs> ItemAdded;

        public FolderDirectoryItem(string name, IFolderDirectoryItem parentFolder)
            : this(new DirectoryFileInfo(name), parentFolder)
        {

        }

        public FolderDirectoryItem(IFileInfo directoryFileInfo, IFolderDirectoryItem parentFolder)
            : this(directoryFileInfo, parentFolder, true)
        {
            Items = new Dictionary<string, IDirectoryItem>();
        }

        protected FolderDirectoryItem(IFileInfo fileInfo, IFolderDirectoryItem parentFolder, bool listenToParent) : base(fileInfo, parentFolder, listenToParent)
        {

        }

        protected override void OnParentUpdated(object sender, DirectoryItemUpdatedEventArgs e)
        {
            // If the parent path changes (i.e folder rename?), 
            // or its existence changes, it effects us so we need to notify subscribers we have been affected!
            if ((e.OldItem.Path != e.NewItem.Path))
            {
                var oldItem = new FolderDirectoryItem(this.FileInfo, e.OldItem as IFolderDirectoryItem, false);
                OnRaiseItemUpdated(oldItem);
            }
        }

        #region IFolderDirectoryItem

        public Dictionary<string, IDirectoryItem> Items { get; set; }

        public override bool IsFolder { get { return true; } }

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

        public IFileDirectoryItem AddOrUpdateFile(IFileInfo file)
        {
            var existingItem = GetFileItem(file.Name);
            if (existingItem == null)
            {
                return AddFile(file);
            }
            else
            {
                existingItem.Update(file);
                return existingItem;
            }
        }

        /// <summary>
        /// Updates the existing item in the directory, overwriting it..
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public IFileDirectoryItem UpdateFile(IFileInfo file)
        {
            var existingItem = GetFileItem(file.Name);
            existingItem.Update(file);
            return existingItem;
        }

        public void ReplaceItem(IDirectoryItem existingItem, IFileInfo newItem)
        {
            if (Items.ContainsKey(newItem.Name))
            {
                throw new InvalidOperationException("Cannot rename item as an item already exists with the same name.");
            }

            if (existingItem != null)
            {
                Items.Remove(existingItem.Name);
            }
            Items[newItem.Name] = existingItem;
            existingItem.ApplyUpdate(newItem);

        }

        //public void OnRenamed(string newName)
        //{
        //    var newDirItem = new DirectoryFileInfo(newName);
        //    Update(newDirItem);
        //}

        public override void Update(IFileInfo newFileInfo)
        {

            if (this.ParentFolder != null)
            {
                if (FileInfo.Name != newFileInfo.Name)
                {
                    this.ParentFolder.ReplaceItem(this, newFileInfo);
                    return;
                    //  OnRenamed(oldItem.Name, newFileInfo.Name);
                }
                // Update(newDirItem);
            }

            ApplyUpdate(newFileInfo);

            //var oldItem = new FolderDirectoryItem(this.FileInfo, this.ParentFolder, false);
            //FileInfo = newFileInfo;
            //OnRaiseItemUpdated(oldItem);
        }

        public override void ApplyUpdate(IFileInfo newFileInfo)
        {
            var oldItem = new FolderDirectoryItem(this.FileInfo, this.ParentFolder, false);
            FileInfo = newFileInfo;
            OnRaiseItemUpdated(oldItem);
        }


        //public void OnUpdated(IFileInfo newFileInfo)
        //{
        //    var oldItem = new FolderDirectoryItem(this.FileInfo, this.ParentFolder, false);
        //    FileInfo = newFileInfo;
        //    OnRaiseItemUpdated(oldItem);
        //}

        /// <summary>
        /// Deletes an empty folder.
        /// </summary>
        /// <param name="recursive"></param>
        public override void Delete()
        {
            Delete(false);
        }

        /// <summary>
        /// Deletes the folder. If recursive is specified, then will also delete all of its contents.
        /// </summary>
        /// <param name="recursive"></param>
        public void Delete(bool recursive)
        {
            if (Items != null && Items.Any())
            {
                if (!recursive)
                {
                    throw new InvalidOperationException("Cannot delete a non empty folder, unless you specify a recursive delete.");
                }
            }


            foreach (var item in Items.ToArray()) // We ToArray because Items gets modified during a Delete() call.
            {
                if (item.Value.IsFolder)
                {
                    var folder = item.Value as IFolderDirectoryItem;
                    if (folder != null)
                    {
                        folder.Delete(recursive);
                    }
                }
                else
                {
                    item.Value.Delete();
                }
            }

            //the parent calls on deleted once the child is removed.
            if (this.ParentFolder != null)
            {
                this.ParentFolder.RemoveItem(this.Name);
            }
            else
            {
                // root folder
                OnRemoved();
            }
        }

        private IFileDirectoryItem GetFileItem(string name)
        {
            if (Items.ContainsKey(name))
            {
                var existing = Items[name];
                if (!existing.IsFolder)
                {
                    return existing as IFileDirectoryItem;
                }
            }

            return null;

            //throw new InvalidOperationException("No such file exists.");
        }

        /// <summary>
        /// returns the item in the folder ith the specified name, or null if it doesn't exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The item in the folder, or nulll if it doesn't exist.</returns>
        public IDirectoryItem NavigateToNext(string name)
        {
            if (name.Equals(".."))
            {
                return this.ParentFolder;
            }

            if (name.Equals("."))
            {
                return this;
            }

            if (Items.ContainsKey(name))
            {
                var existing = Items[name];
                return existing;
            }

            return null;

        }

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

            // todo: could put in check here for deleting folders with items.. rather than in delete method.

            var result = Items.Remove(name);
            if (result)
            {
                existing.OnRemoved();
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

        public override void Accept(BaseDirectoryVisitor Visitor)
        {
            Visitor.Visit(this);

            // visit children?
            // allow visitor to decide.

        }

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



    }
}
