using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public interface IDirectory
    {

        bool TryGetItem(string path, out IDirectoryItem item);
        bool TryGetFile(string path, out IFileDirectoryItem fileItem);
        bool TryGetFolder(string path, out IFolderDirectoryItem fileItem);

        IFolderDirectoryItem GetOrAddDirectory(string directory);

        IFolderDirectoryItem Root { get; }

        IEnumerable<IDirectoryItem> Search(string globPattern);

        IFileDirectoryItem AddFile(string directory, IFileInfo file);


     

    }
}