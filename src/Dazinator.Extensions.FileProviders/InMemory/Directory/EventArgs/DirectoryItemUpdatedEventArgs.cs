using System;

namespace Dazinator.Extensions.FileProviders.InMemory.Directory
{
    public class DirectoryItemUpdatedEventArgs : EventArgs
    {
        public DirectoryItemUpdatedEventArgs(IDirectoryItem oldItem, IDirectoryItem newItem)
        {
            OldItem = oldItem;
            NewItem = newItem;
        }
        public IDirectoryItem OldItem { get; private set; }
        public IDirectoryItem NewItem { get; private set; }
    }
}