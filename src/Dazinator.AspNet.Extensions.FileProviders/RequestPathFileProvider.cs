using Dazinator.AspNet.Extensions.FileProviders.Directory;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.IO;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    /// <summary>
    /// Maps request to an underlying file provider, but handles an additional path that can be prepended to the subpath.
    /// </summary>
    public class RequestPathFileProvider : IFileProvider
    {

        private readonly PathString _basePath;
        private readonly IFileProvider _underlyingFileProvider;
        private readonly IFileInfo _baseDirectoryFileInfo;
        private static readonly char[] _splitChar = new char[1] { '/' };

        public RequestPathFileProvider(string basePath, IFileProvider underlyingFileProvider)
        {
            _basePath = new PathString(basePath);
            _baseDirectoryFileInfo = new DirectoryFileInfo(_basePath.ToString().TrimStart(_splitChar));
            _underlyingFileProvider = underlyingFileProvider;
        }

        protected virtual bool TryMapSubPath(string originalSubPath, out PathString newSubPath)
        {
            if (!string.IsNullOrEmpty(originalSubPath))
            {
                PathString originalPathString;
                if (originalSubPath[0] != '/')
                {
                    originalPathString = new PathString('/' + originalSubPath);
                }
                else
                {
                    originalPathString = new PathString(originalSubPath);
                }

                PathString remaining;
                if (originalPathString.HasValue && originalPathString.StartsWithSegments(_basePath, out remaining))
                {
                    // var childPath = originalPathString.Remove(0, _basePath.Value.Length);
                    newSubPath = remaining;
                    return true;
                }
            }

            newSubPath = null;
            return false;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (string.IsNullOrEmpty(subpath))
            {
                // return root / base directory.
                return new EnumerableDirectoryContents(_baseDirectoryFileInfo);
            }
            PathString newPath;
            if (TryMapSubPath(subpath, out newPath))
            {
                var contents = _underlyingFileProvider.GetDirectoryContents(newPath);
                return contents;
            }

            return new NotFoundDirectoryContents();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            PathString newPath;
            if (TryMapSubPath(subpath, out newPath))
            {
                var result = _underlyingFileProvider.GetFileInfo(newPath);
                return result;
            }

            return new NotFoundFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            // We check if the pattern starts with the base path, and remove it if necessary.
            // otherwise we just pass the pattern through unaltered.
            PathString newPath;
            if (TryMapSubPath(filter, out newPath))
            {
                var result = _underlyingFileProvider.Watch(newPath);
                return result;
            }

            return _underlyingFileProvider.Watch(newPath);
            //  return NullChangeToken.Singleton;
        }
    }
}
