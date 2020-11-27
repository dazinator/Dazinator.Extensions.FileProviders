using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Dazinator.Extensions.FileProviders.InMemory.Directory
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
            Items = new ConcurrentDictionary<string, IDirectoryItem>();
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
                FolderDirectoryItem oldItem = new FolderDirectoryItem(FileInfo, e.OldItem as IFolderDirectoryItem, false);
                OnRaiseItemUpdated(oldItem);
            }
        }

        #region IFolderDirectoryItem

        public ConcurrentDictionary<string, IDirectoryItem> Items { get; set; }

        public override bool IsFolder => true;

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
        /// Adds ther file to the folder, overwriting the file if it exists..
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>       
        public IFileDirectoryItem AddFile(IFileInfo file)
        {
            string name = file.Name;
            FileDirectoryItem newItem = new FileDirectoryItem(file, this);
            if (!AddItem(name, newItem))
            {
                throw new InvalidOperationException("Cannot add item to the directory, as an item with the same name already exists.");
            }
            return newItem;
        }

        public IFileDirectoryItem AddOrUpdateFile(IFileInfo file)
        {

            string name = file.Name;
            FileDirectoryItem newItem = new FileDirectoryItem(file, this);
            AddOrUpdateItem(name, newItem);
            return newItem;
        }

        /// <summary>
        /// Updates the existing item in the directory, overwriting it..
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public IFileDirectoryItem UpdateFile(IFileInfo file)
        {
            IFileDirectoryItem existingItem = GetFileItem(file.Name);
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
                Items.TryRemove(existingItem.Name, out IDirectoryItem removedItem);
            }

            Items[newItem.Name] = existingItem;
            existingItem.ApplyUpdate(newItem);

        }

        public override void Update(IFileInfo newFileInfo)
        {

            if (ParentFolder != null)
            {
                if (FileInfo.Name != newFileInfo.Name)
                {
                    ParentFolder.ReplaceItem(this, newFileInfo);
                    return;
                }
            }

            ApplyUpdate(newFileInfo);
        }

        public override void ApplyUpdate(IFileInfo newFileInfo)
        {
            FolderDirectoryItem oldItem = new FolderDirectoryItem(FileInfo, ParentFolder, false);
            FileInfo = newFileInfo;
            OnRaiseItemUpdated(oldItem);
        }

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

            foreach (KeyValuePair<string, IDirectoryItem> item in Items.ToArray()) // We ToArray because Items gets modified during a Delete() call.
            {
                if (item.Value.IsFolder)
                {
                    IFolderDirectoryItem folder = item.Value as IFolderDirectoryItem;
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
            if (ParentFolder != null)
            {
                ParentFolder.RemoveItem(Name);
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
                IDirectoryItem existing = Items[name];
                if (!existing.IsFolder)
                {
                    return existing as IFileDirectoryItem;
                }
            }

            return null;
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
                return ParentFolder;
            }

            if (name.Equals("."))
            {
                return this;
            }

            if (Items.ContainsKey(name))
            {
                IDirectoryItem existing = Items[name];
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

            IDirectoryItem existing = Items.GetOrAdd(name, (n) =>
            {
                IDirectoryItem newFolder = createItemCallback(n);
                OnRaiseItemAdded(newFolder);
                return newFolder;
            });

            return existing;
        }

        /// <summary>
        /// Adds an item to folder directory.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private IDirectoryItem AddOrUpdateItem(string name, IDirectoryItem newItem)
        {

            Items.AddOrUpdate(name, (n) =>
            {
                OnRaiseItemAdded(newItem);
                return newItem;
            }, (s, existing) =>
            {
                if (existing.IsFolder)
                {
                    IFolderDirectoryItem existingFolder = (IFolderDirectoryItem)existing;
                    if (newItem.IsFolder)
                    {
                        IFolderDirectoryItem newFolder = (IFolderDirectoryItem)newItem;
                        foreach (IDirectoryItem newFile in newFolder)
                        {
                            existingFolder.AddOrUpdateFile(newFile.FileInfo);
                        }
                    }
                    else
                    {
                        existingFolder.AddOrUpdateFile(newItem.FileInfo);
                    }
                }
                else
                {
                    if (newItem.IsFolder)
                    {
                        throw new Exception("Cannot add folder to directory as a file already exists with the same name.");
                    }
                    else
                    {
                        IFileDirectoryItem existingFileItem = (IFileDirectoryItem)existing;
                        existingFileItem.Update(newItem.FileInfo);
                    }
                }
                return existing;
            });

            return newItem;
        }

        private bool AddItem(string name, IDirectoryItem newItem)
        {
            if (Items.TryAdd(name, newItem))
            {
                OnRaiseItemAdded(newItem);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Removes an item from the folder directory.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool RemoveItem(string name)
        {
            IDirectoryItem existing = Items[name];

            // todo: could put in check here for deleting folders with items.. rather than in delete method.
            bool result = Items.TryRemove(name, out IDirectoryItem removed);
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
                DirectoryItemAddedEventArgs args = new DirectoryItemAddedEventArgs(newItem);

                // Use the () operator to raise the event.
                handler(this, args);
            }
        }

        public override void Accept(BaseDirectoryVisitor Visitor)
        {
            Visitor.Visit(this);
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
