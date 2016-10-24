using System;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public class FileDirectoryItem : IFileDirectoryItem
    {
        public FileDirectoryItem(IFileInfo fileInfo, IFolderDirectoryItem parentFolder)
        {
            FileInfo = fileInfo;
            ParentFolder = parentFolder;
        }

        public IFileInfo FileInfo { get; private set; }

        public string Path => ParentFolder?.Path + "/" + Name;
        public bool IsFolder => false;
        public IDirectoryItem GetChildDirectoryItem(string name)
        {
            // files in a directory cannot have child items.
            return null;
        }

        public string Name => FileInfo.Name;

        public IFolderDirectoryItem ParentFolder { get; set; }

        public event EventHandler<DirectoryItemUpdatedEventArgs> Updated;
        public event EventHandler<DirectoryItemDeletedEventArgs> Deleted;
        
        public void Update(IFileInfo newFileInfo)
        {
            // take a snapshot of current directory item with the old file.
            var oldItem = new FileDirectoryItem(this.FileInfo, this.ParentFolder);
            // now change the file to the new file on this item.
            FileInfo = newFileInfo;
            // now signal the file has changed.
            OnRaiseItemUpdated(oldItem);
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

        protected virtual void OnRaiseItemUpdated(IDirectoryItem oldItem)
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

      

    }
}