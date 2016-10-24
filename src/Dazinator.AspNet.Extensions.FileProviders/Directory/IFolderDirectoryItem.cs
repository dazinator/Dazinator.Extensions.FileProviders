using System;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public interface IFolderDirectoryItem : IDirectoryItem, IEnumerable<IDirectoryItem>
    {
        IFolderDirectoryItem GetOrAddFolder(string name);

        IFileDirectoryItem AddFile(IFileInfo file);

        IFileDirectoryItem UpdateFile(IFileInfo file);

       // bool DeleteItem(string name);
      
        event EventHandler<DirectoryItemAddedEventArgs> ItemAdded;

        bool RemoveItem(string name);
    }
}