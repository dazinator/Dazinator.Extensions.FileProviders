using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Dazinator.Extensions.FileProviders
{
    public class EnumerableDirectoryContents : IDirectoryContents
    {
        private readonly bool _exists;
        private readonly IFileInfo[] _files;

        public EnumerableDirectoryContents(bool exists, params IFileInfo[] files)
        {
            _exists = exists;
            _files = files;
        }

        public bool Exists => _exists;

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            foreach (var entry in _files)
            {
                yield return entry;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

}
