using System;
using System.Collections.Generic;
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

            _map.TryNavigateTo(pathString, out var candidate, out var remaining, out var segments);

            while (candidate != null)
            {

                if (candidate.TryGetMappedFile(remaining, segments, out var sourceFileInfo))
                {
                    if (sourceFileInfo.Exists && !sourceFileInfo.IsDirectory)
                    {
                        return sourceFileInfo;
                    }
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

            _map.TryNavigateTo(pathString, out var candidate, out var remaining, out var segments);

            var directoryContents = new List<IDirectoryContents>(segments.Length);
            while (candidate != null)
            {
                // need to recurse back from top level directory through to root
                // at each level, we include directory contents pattern mapped at that level.
                // so for example, /foo/bar/bat could be mapped to provider A
                // whilst /foo could have a pattern mapping that includes /bar/bat folder from provider B
                // we need A's to take precendence, but for B's contents of that directory to also be added
                // and to do this at each level back to the root to build the full directory contents.
                // Note: This isn't necessary when resolving files, as we can stop at the first file we find when working back
                // towards the root segment in this way.
                if (candidate.GetMappedDirectoryContents(remaining, segments, out var contents))
                {
                    directoryContents.Add(contents);
                }

                if (candidate.Parent != null)
                {
                    remaining = candidate.Path.Add(remaining);
                }

                candidate = candidate.Parent;
            }

            if (directoryContents.Count > 0)
            {
                return new CompositeDirectoryContents(directoryContents);
            }

            return NotFoundDirectoryContents.Singleton;

        }

        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }
    }
}
