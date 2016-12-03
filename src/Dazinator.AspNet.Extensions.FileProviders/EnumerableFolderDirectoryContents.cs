using Dazinator.AspNet.Extensions.FileProviders.Directory;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
