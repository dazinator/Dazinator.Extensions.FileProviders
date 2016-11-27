using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{

    public interface IDirectory : IVisitable<BaseDirectoryVisitor>
    {
        IFolderDirectoryItem Root { get; }

        IDirectoryItem GetItem(string path);
        IFileDirectoryItem GetFile(string path);
        IFileDirectoryItem AddFile(string directory, IFileInfo file);
        IFileDirectoryItem AddOrUpdateFile(string directory, IFileInfo file);
        IFolderDirectoryItem GetFolder(string path);
        IFolderDirectoryItem GetOrAddFolder(string directory);
        IEnumerable<IDirectoryItem> Search(string globPattern);
      

    }
}