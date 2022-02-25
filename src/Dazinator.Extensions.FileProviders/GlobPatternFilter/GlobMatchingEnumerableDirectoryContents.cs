using Microsoft.Extensions.FileProviders;
using System.Collections;
using System.Collections.Generic;

namespace Dazinator.Extensions.FileProviders.GlobPatternFilter
{
    public class GlobMatchingEnumerableDirectoryContents : IDirectoryContents
    {
        private readonly GlobMatchingEnumerableFileInfos _globAllowedFiles;

        public GlobMatchingEnumerableDirectoryContents(GlobMatchingEnumerableFileInfos globAllowedFiles)
        {
            _globAllowedFiles = globAllowedFiles;
        }

        public bool Exists => true;

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            foreach (var entry in _globAllowedFiles)
            {
                yield return entry.Item2;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }


}
