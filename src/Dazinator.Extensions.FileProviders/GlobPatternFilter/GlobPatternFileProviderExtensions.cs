using Dazinator.Extensions.FileProviders.GlobPatternFilter;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dazinator.Extensions.FileProviders
{
    // ReSharper disable once CheckNamespace
    // Extension method put in root namespace for discoverability purposes.
    public static class GlobPatternFileProviderExtensions
    {
        /// <summary>
        /// returns all items in the directory that match the specified glob pattern, starting from <paramref name="startDir"/>.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, IFileInfo>> Search(this IFileProvider fileProvider, string globPattern)
        {
            var results = new GlobMatchingEnumerableFileInfos("", true, fileProvider, globPattern);
            return results;
        }


        /// <summary>
        /// returns all items in the directory that match the specified glob pattern.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, IFileInfo>> Search(this IFileProvider fileProvider, params string[] includePatterns)
        {
            var results = new GlobMatchingEnumerableFileInfos("", true, fileProvider, includePatterns);
            return results;
        }
     

        /// <summary>
        /// returns all items in the directory that match the specified glob pattern.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, IFileInfo>> Search(this IFileProvider fileProvider, string[] includePatterns, params string[] excludePatterns)
        {
            var results = new GlobMatchingEnumerableFileInfos("", true, fileProvider, includePatterns, excludePatterns);
            return results;
        }

    }
}
