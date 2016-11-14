using System;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public interface IFolderDirectoryItem : IDirectoryItem, IEnumerable<IDirectoryItem>
    {
        IFolderDirectoryItem GetOrAddFolder(string name);

        void Rename(string newName);

        IFileDirectoryItem AddFile(IFileInfo file);

        IFileDirectoryItem UpdateFile(IFileInfo file);

        void Update(IFileInfo file);
        
        /// <summary>
        /// Deletes an empty folder, or a folder and all its contents if recursive is specified.
        /// </summary>
        void Delete(bool recursive);

        event EventHandler<DirectoryItemAddedEventArgs> ItemAdded;

        bool RemoveItem(string name);
    }
}