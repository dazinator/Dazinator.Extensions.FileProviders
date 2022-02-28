using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Dazinator.Extensions.FileProviders.Mapping
{
    /// <summary>
    /// A file provider that maps paths to files from other providers, 
    /// using explicit mappings between source file names and target provider file names
    /// as well as "pattern" mappings which match remaining segments of a path to a file provider responsible
    /// for all matches for that pattern under that segment.
    /// </summary>
    public class MappingFileProvider : IFileProvider
    {
        private FileMap _map;

        public MappingFileProvider(FileMap map)
        {
            _map = map;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (subpath == null)
            {
                throw new ArgumentNullException(nameof(subpath));
            }

            PathString pathString;
            if (subpath.StartsWith('/'))
            {
                pathString = subpath;
            }
            else
            {
                pathString = $"/{subpath}";
            }

            _map.TryNavigateTo(pathString, out var candidate, out var remaining);

            while (candidate != null)
            {

                if (candidate.TryGetMappedFile(remaining, pathString, out var sourceFileInfo))
                {
                    return sourceFileInfo;
                }

                if (candidate.Parent != null)
                {
                    remaining = candidate.Path.Add(remaining);
                }

                candidate = candidate.Parent;

            }

            return new NotFoundFileInfo(subpath);

        }
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (subpath == null)
            {
                throw new ArgumentNullException(nameof(subpath));
            }

            PathString pathString;
            if (subpath.StartsWith('/'))
            {
                pathString = subpath;
            }
            else
            {
                pathString = $"/{subpath}";
            }

            if (_map.TryNavigateTo(pathString, out var candidate, out var remaining))
            {
                // we have mappings for this path, they take precedence over any patterns higher up


                // get any files from explicit file mappings on the current path,
                // plus any items matched
                var mappedDirectoryContents = candidate.GetMappedDirectoryContents();
                return mappedDirectoryContents;

            }

            while (candidate != null)
            {
                if (candidate.Parent != null)
                {
                    remaining = candidate.Path.Add(remaining);
                }

                candidate = candidate.Parent;
            }

            throw new NotImplementedException();
        }
        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }
    }
}
