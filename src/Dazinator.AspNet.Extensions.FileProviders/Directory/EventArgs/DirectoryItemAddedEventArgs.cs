using System;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
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