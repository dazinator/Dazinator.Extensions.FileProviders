using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Dazinator.Extensions.FileProviders.PrependBasePath
{
    /// <summary>
    /// Prepends a base path to files / directories from an underlying file provider.
    /// </summary>
    public class PrependBasePathFileProvider : IFileProvider
    {

        private readonly PathString _basePath;
        private readonly IFileProvider _underlyingFileProvider;
        private readonly IFileInfo _baseDirectoryFileInfo;
        private static readonly char[] _splitChar = new char[1] { '/' };

        public PrependBasePathFileProvider(string basePath, IFileProvider underlyingFileProvider)
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
                if (originalPathString.HasValue &&
                    originalPathString.StartsWithSegments(_basePath, out remaining))
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
                return new EnumerableDirectoryContents(true, _baseDirectoryFileInfo);
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
                var result = _underlyingFileProvider.GetFileInfo(newPath.Value);
                return result;
            }

            return new NotFoundFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            // We check if the pattern starts with the base path, and remove that portion if necessary before passing the rest to the
            // underlying provider (as the base path prefix is fictitious and not really part of the path of any files in the underlyign provider so passing the pattern as is would not match anything)

            // otherwise if the pattern does not start with our fictitious base path,
            // we end up passing null as the pattern (which shouldn't match anything in the underlying provider?)

            // note: thios would mean you couldn't use patterns like "**/" but instead have to use "/[basepath]/**" for them to work with this provider
            // todo: this should probably be documented if not alrready.. 
            // not sure if this is desirable behaviour or not, perhaps it would be better to pass down the filter in this situation even though it doesn't start with our base path ?

            PathString newPath;
            if (TryMapSubPath(filter, out newPath))
            {
                var result = _underlyingFileProvider.Watch(newPath.Value);
                return result;
            }

            return _underlyingFileProvider.Watch(newPath);
            //  return NullChangeToken.Singleton;
        }
    }
}
