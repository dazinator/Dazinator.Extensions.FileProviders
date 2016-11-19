using System;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public class FileDirectoryItem : BaseDirectoryItem, IFileDirectoryItem
    {

        public FileDirectoryItem(IFileInfo fileInfo, IFolderDirectoryItem parentFolder) : this(fileInfo, parentFolder, true)
        {
            
        }

        protected FileDirectoryItem(IFileInfo fileInfo, IFolderDirectoryItem parentFolder, bool listenToParent) : base(fileInfo, parentFolder, listenToParent)
        {

        }


        protected override void OnParentUpdated(object sender, DirectoryItemUpdatedEventArgs e)
        {
            // If the parent path changes (i.e folder rename?), 
            // or its existence changes, it effects us so we need to notify subscribers we have been affected!
            if ((e.OldItem.Path != e.NewItem.Path))
            {
                var oldItem = new FileDirectoryItem(this.FileInfo, e.OldItem as IFolderDirectoryItem, false);
                OnRaiseItemUpdated(oldItem);
            }
        }

        public override bool IsFolder { get { return false; } }

        public override void Update(IFileInfo newFileInfo)
        {
            // If this is a rename, ensure parent is notified, as we need to be re-indexed by the parent folder.
            if (this.ParentFolder != null)
            {
                if (FileInfo.Name != newFileInfo.Name)
                {
                    this.ParentFolder.ReplaceItem(this, newFileInfo);
                    return; // parent will call OnReplaced();
                    //  OnRenamed(oldItem.Name, newFileInfo.Name);
                }
                // Update(newDirItem);
            }

            ApplyUpdate(newFileInfo);
           
        }
        
        public override void ApplyUpdate(IFileInfo newFileInfo)
        {// take a snapshot of current directory item with the old file.
            var oldItem = new FileDirectoryItem(this.FileInfo, this.ParentFolder, false);
            // now change the file to the new file on this item.
            FileInfo = newFileInfo;
            // now signal the file has changed.
            OnRaiseItemUpdated(oldItem);
        }

        public override void Accept(BaseDirectoryVisitor Visitor)
        {
            Visitor.Visit(this);
        }

      
    }
}