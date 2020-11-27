using System;

namespace Dazinator.Extensions.FileProviders.InMemory.Directory
{
    public class DirectoryItemAddedEventArgs : EventArgs
    {
        public DirectoryItemAddedEventArgs(IDirectoryItem newItem)
        {
            NewItem = newItem;
        }
        public IDirectoryItem NewItem { get; private set; }
    }
}