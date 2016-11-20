using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public abstract class BaseDirectoryItem : IDirectoryItem
    {
        public event EventHandler<DirectoryItemUpdatedEventArgs> Updated;
        public event EventHandler<DirectoryItemDeletedEventArgs> Deleted;
        private readonly bool _listenToParent = false;

        protected BaseDirectoryItem(IFileInfo fileInfo, IFolderDirectoryItem parentFolder, bool listenToParent)
        {
            _listenToParent = listenToParent;
            FileInfo = fileInfo;
            ParentFolder = parentFolder;
        }

        private void EnsureParentEvents(IFolderDirectoryItem parent, bool listen)
        {


            if (_listenToParent)
            {
                if (parent == null)
                {
                    throw new ArgumentNullException("parent");
                }

                if (listen)
                {
                    parent.Updated += OnParentUpdated;
                }
                else
                {
                    parent.Updated -= OnParentUpdated;
                }
            }

        }

        protected abstract void OnParentUpdated(object sender, DirectoryItemUpdatedEventArgs e);

        #region IDirectoryItem

        public void OnRemoved()
        {
            FileInfo = new NotFoundFileInfo(this.FileInfo.Name);
            OnRaiseItemDeleted();
            ParentFolder = null; // no need to monitor parent anymore as we have been removed from the directory.
        }

        private IFolderDirectoryItem _parentFolder;
        public IFolderDirectoryItem ParentFolder
        {
            get
            {
                return _parentFolder;
            }
            set
            {
                var old = _parentFolder;
                _parentFolder = value;

                if (old != null)
                {
                    EnsureParentEvents(old, false);
                }

                if (_parentFolder != null)
                {
                    EnsureParentEvents(_parentFolder, true);
                }

            }
        }

        public virtual string Name => FileInfo.Name;

        public abstract bool IsFolder { get; }

        public virtual string Path => ParentFolder == null ? Name : ParentFolder.Path + "/" + Name;

        public virtual IFileInfo FileInfo { get; protected set; }

        /// <summary>
        /// Deletes an empty folder.
        /// </summary>
        /// <param name="recursive"></param>
        public virtual void Delete()
        {
            if (this.ParentFolder != null)
            {
                this.ParentFolder.RemoveItem(this.Name);
            }
        }

        public abstract void Accept(BaseDirectoryVisitor Visitor);
        // public event EventHandler<DirectoryItemDeletedEventArgs> ItemDeleted;

        #endregion

        protected void OnRaiseItemUpdated(IDirectoryItem oldItem)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<DirectoryItemUpdatedEventArgs> handler = Updated;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                var args = new DirectoryItemUpdatedEventArgs(oldItem, this);

                // Use the () operator to raise the event.
                handler(this, args);
            }
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

        public abstract void Update(IFileInfo newFileInfo);

        public abstract void ApplyUpdate(IFileInfo newFileInfo);

        public virtual void Rename(string newName)
        {
            var newItem = new WrappedFileInfo(FileInfo);
            newItem.Name = newName;
            Update(newItem);
        }


    }
}
