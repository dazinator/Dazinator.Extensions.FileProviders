using Microsoft.Extensions.FileProviders;
using System.Collections;
using System.Collections.Generic;
using Dazinator.AspNet.Extensions.FileProviders.Directory;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    public class EnumerableFolderDirectoryContents : IDirectoryContents
    {
        private readonly IFolderDirectoryItem _folder;

        public EnumerableFolderDirectoryContents(IFolderDirectoryItem folder)
        {
            _folder = folder;
        }

        public bool Exists => true;
       
        public IEnumerator<IFileInfo> GetEnumerator()
        {
            foreach (var entry in _folder)
            {
                yield return entry.FileInfo;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
