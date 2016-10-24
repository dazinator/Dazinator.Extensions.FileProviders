using System;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
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