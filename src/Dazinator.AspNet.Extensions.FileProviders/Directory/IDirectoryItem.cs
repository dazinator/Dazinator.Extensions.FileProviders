using System;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public interface IDirectoryItem
    {
        IFolderDirectoryItem ParentFolder { get; set; }

        string Name { get; }

        bool IsFolder { get; }

        /// <summary>
        /// returns the next item from the directory based on its name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IDirectoryItem GetChildDirectoryItem(string name);

        IFileInfo FileInfo { get; }

        string Path { get; }

        event EventHandler<DirectoryItemUpdatedEventArgs> Updated;
        event EventHandler<DirectoryItemDeletedEventArgs> Deleted;

        void OnDeleted();

        /// <summary>
        /// Deletes the item from the directory.
        /// </summary>
        void Delete();



    }
}