using System;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public interface IFileDirectoryItem : IDirectoryItem
    {
        void Update(IFileInfo newFileInfo);
        //void Delete();

    }
}