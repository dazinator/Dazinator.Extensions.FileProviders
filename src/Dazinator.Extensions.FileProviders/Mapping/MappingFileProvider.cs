using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using static Dazinator.Extensions.FileProviders.Mapping.FileMap;

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

        private static char[] _splitChars = new char[] { '/' };

        public IFileInfo GetFileInfo(string subpath)
        {
            // traverse each segment of the subath to get far as we can
            var candidate = _map;

            var segments = subpath.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries);
            int depth = 0;

            //PathString matchedPath = "/";

            for (var i = 0; i < segments.Length; i++)
            {
                if (candidate.TryGetChild($"/{segments[i]}", out var child))
                {
                    candidate = child;
                    depth = depth + 1;
                    continue;
                }
                break;
            }

            // check for explicit file mapping first

            //    segments.Length == 0 ? segments[0] : segments[segments.Length - 1];
            var next = candidate;

            // other wise fall back to pattern checks,
            // check nearest patterns, then walk backwards towards root.
            // PathString fileNamePathString = $"/{fileName}";
            var remainingPath = new PathString($"/{string.Join("/", segments[depth..])}");

            while (next != null)
            {

                if (next.TryGetMappedFile(remainingPath, out var sourceFileInfo))
                {
                    return sourceFileInfo;
                }

                // todo: not certain about this, leaving for now to see if any issue can be proven.
                //// scenario:
                //// the file: /foo/bar/bat.txt could be represented as
                //// nodes: - foo
                ////           - bar
                //// with a file mapping on bar for source file "bat.txt" provided by file provider A.
                //// but it also could be represented as
                //// nodes: - foo
                //// with a file mapping on foo for source file "/bar/bat.txt" provided by file provider B.
                //// we've already covered the first case, however in this loop we are covering the second case
                //// as it presents itself as we walk back through the hierarchy
                //if (candidate.TryGetFileMapping($"/{fileName}", out sourceFile))
                //{
                //    return sourceFile.GetFileInfo();
                //}
                if (next.Parent != null)
                {
                    remainingPath = next.Path.Add(remainingPath);
                }

                next = next.Parent;

            }

            return new NotFoundFileInfo(subpath);

        }
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotImplementedException();
        }
        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }
    }
}
