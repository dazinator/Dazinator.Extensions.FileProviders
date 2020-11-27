using System;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.Extensions.FileProviders.InMemory.Directory
{
    public interface IFileDirectoryItem : IDirectoryItem
    {
        void Update(IFileInfo newFileInfo);
        //void Delete();

    }
}