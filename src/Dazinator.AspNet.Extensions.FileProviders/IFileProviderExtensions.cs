using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    // ReSharper disable once CheckNamespace
    // Extension method put in root namespace for discoverability purposes.
    public static class IFileProviderExtensions
    {
        public static IFileInfo EnsureFile(this IFileProvider fileProvider, string path)
        {
            var file = fileProvider.GetFileInfo(path);
            if (file.Exists)
            {
                if (file.IsDirectory)
                {
                    throw new FileNotFoundException($"The specified path was a directory, but expected a file path: {0}", path);
                }
                return file;
            }
            else
            {
                throw new FileNotFoundException($"No such file exists: {0}", path);
            }
        }

        public static string ReadAllContent(this IFileInfo fileInfo)
        {
            using (var reader = new StreamReader(fileInfo.CreateReadStream()))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// returns all items in the directory that match the specified glob pattern.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, IFileInfo>> Search(this IFileProvider fileProvider, string globPattern)
        {
            var results = new GlobMatchingEnumerableFileInfos(fileProvider, globPattern);
            return results;
        }


        /// <summary>
        /// returns all items in the directory that match the specified glob pattern.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, IFileInfo>> Search(this IFileProvider fileProvider, params string[] includePatterns)
        {
            var results = new GlobMatchingEnumerableFileInfos(fileProvider, includePatterns);
            return results;
        }

        /// <summary>
        /// returns all items in the directory that match the specified glob pattern.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, IFileInfo>> Search(this IFileProvider fileProvider, string[] includePatterns, params string[] excludePatterns)
        {
            var results = new GlobMatchingEnumerableFileInfos(fileProvider, includePatterns, excludePatterns);
            return results;
        }

    }
}
