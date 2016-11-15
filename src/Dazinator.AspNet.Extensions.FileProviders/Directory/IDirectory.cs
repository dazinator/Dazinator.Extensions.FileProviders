using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public class BaseDirectoryVisitor
    {
        public virtual void Visit(FileDirectoryItem item)
        {
            // no op.
        }

        public virtual void Visit(FolderDirectoryItem item)
        {
            // no op.
        }

    }


    public interface IVisitable<T>
    {
        void Accept(T Visitor);
    }

    public interface IDirectory : IVisitable<BaseDirectoryVisitor>
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