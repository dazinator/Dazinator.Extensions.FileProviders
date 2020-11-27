using System;

namespace Dazinator.Extensions.FileProviders.InMemory.Directory
{
    public class DirectoryItemDeletedEventArgs : EventArgs
    {
        public DirectoryItemDeletedEventArgs(IDirectoryItem deletedItem)
        {
            DeletedItem = deletedItem;
        }
        public IDirectoryItem DeletedItem { get; private set; }
    }
}