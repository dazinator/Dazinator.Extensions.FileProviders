using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public interface IDirectory
    {

        IDirectoryItem GetItem(string path);
        IFileDirectoryItem GetFile(string path);
        IFolderDirectoryItem GetFolder(string path);
        IFolderDirectoryItem GetOrAddFolder(string directory);
        IFolderDirectoryItem Root { get; }
        IEnumerable<IDirectoryItem> Search(string globPattern);
        IFileDirectoryItem AddFile(string directory, IFileInfo file);

    }
}