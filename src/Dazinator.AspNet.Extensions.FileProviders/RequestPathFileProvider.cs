using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    /// <summary>
    /// Maps requests for files to an underlying file provider, based on a flexibile mapping.
    /// </summary>
    public class RequestPathFileProvider : IFileProvider
    {

        private string _basePath;
        private IFileProvider _underlyingFileProvider;

        public RequestPathFileProvider(string basePath, IFileProvider underlyingFileProvider)
        {
            _basePath = basePath;
            _underlyingFileProvider = underlyingFileProvider;
        }

        protected virtual bool TryMapSubPath(string originalSubPath, out string newSubPath)
        {
            if (originalSubPath != null && originalSubPath.StartsWith(_basePath))
            {
                var childPath = originalSubPath.Remove(0, _basePath.Length);
                newSubPath = childPath;
                return true;
            }

            newSubPath = null;
            return false;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            string newPath;
            if (TryMapSubPath(subpath, out newPath))
            {
                var contents = _underlyingFileProvider.GetDirectoryContents(newPath);
                return contents;
            }

            return new NotFoundDirectoryContents();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            string newPath;
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
            string newPath;
            if (TryMapSubPath(filter, out newPath))
            {
                var result = _underlyingFileProvider.Watch(newPath);
                return result;
            }

            return _underlyingFileProvider.Watch(newPath);
            //  return NullChangeToken.Singleton;
        }

        public bool Like(string pattern)
        {

            var regex = new Regex("^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var path = this.ToString();

            var isMatch = regex.IsMatch(path);
            return isMatch;
        }
    }
}
