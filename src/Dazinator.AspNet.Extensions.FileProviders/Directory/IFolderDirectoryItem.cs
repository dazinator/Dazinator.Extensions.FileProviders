using System;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public interface IFolderDirectoryItem : IDirectoryItem, IEnumerable<IDirectoryItem>
    {

        /// <summary>
        /// navigates onwards (or backwards) to the next directory item in the path, based on the name specified.
        /// </summary>
        /// <remarks>Use .. to navigate back to the parent directory, or . to stay on the current directory, or the name of a descendent file or folder to navigate onwards.</remarks>
        /// <param name="name"></param>
        /// <returns></returns>
        IDirectoryItem NavigateToNext(string name);

        IFolderDirectoryItem GetOrAddFolder(string name);

        void Rename(string newName);

        void ReplaceItem(IDirectoryItem existingItem, IFileInfo newItem); // don't like this being exposed.

        IFileDirectoryItem AddFile(IFileInfo file);

        IFileDirectoryItem UpdateFile(IFileInfo file);

        void Update(IFileInfo file);

        /// <summary>
        /// Deletes an empty folder, or a folder and all its contents if recursive is specified.
        /// </summary>
        void Delete(bool recursive);

        event EventHandler<DirectoryItemAddedEventArgs> ItemAdded;

        bool RemoveItem(string name);
        IFileDirectoryItem AddOrUpdateFile(IFileInfo file);
    }
}