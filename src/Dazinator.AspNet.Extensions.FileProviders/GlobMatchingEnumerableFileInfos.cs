using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    public class GlobMatchingEnumerableFileInfos : IEnumerable<Tuple<string, IFileInfo>>
    {
        private readonly string _rootDir;
        private readonly bool _recurseDirectories;
        private readonly IFileProvider _fileProvider;
        private readonly GlobPatternIncludeExcludeEvaluator _evaluator;

        public GlobMatchingEnumerableFileInfos(
            string rootDir,
            bool recurseDirectories,
            IFileProvider fileProvider,
            params string[] includePatterns) : this(rootDir, recurseDirectories, fileProvider, includePatterns, null)
        {
        }

        public GlobMatchingEnumerableFileInfos(string rootDir,
            bool recurseDirectories,
            IFileProvider fileProvider,
            string[] includePatterns,
            params string[] excludePatterns) : this(rootDir, recurseDirectories, fileProvider, new GlobPatternIncludeExcludeEvaluator(includePatterns, excludePatterns))
        {
        }

        public GlobMatchingEnumerableFileInfos(string rootDir,
           bool recurseDirectories,
           IFileProvider fileProvider,
           GlobPatternIncludeExcludeEvaluator evaluator)
        {
            _rootDir = rootDir;
            _recurseDirectories = recurseDirectories;
            _fileProvider = fileProvider;
            _evaluator = evaluator;
        }

        public IEnumerator<Tuple<string, IFileInfo>> GetEnumerator()
        {
            var folderPath = _rootDir;
            var currentFolder = _fileProvider.GetDirectoryContents(_rootDir);
            var folders = new Stack<KeyValuePair<string, IFileInfo>>();

            while (currentFolder != null)
            {
                // var basePath = currentFolder.Name
                foreach (var item in currentFolder)
                {
                    var itemPath = $"{folderPath}/{item.Name}";
                    bool isMatch = _evaluator.IsAllowed(itemPath);

                    if (isMatch)
                    {
                        yield return new Tuple<string, IFileInfo>(folderPath, item);
                    }

                    if (item.IsDirectory && _recurseDirectories)
                    {
                        // add the nested folder to a queue for later processing its items in the loop.
                        folders.Push(new KeyValuePair<string, IFileInfo>(itemPath, item));
                        continue;
                    }
                }

                if (folders.Count > 0)
                {
                    var folderItem = folders.Pop();
                    folderPath = folderItem.Key;
                    currentFolder = _fileProvider.GetDirectoryContents(folderPath);
                }
                else
                {
                    // finished iteating directory.
                    currentFolder = null;
                }
            }

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }


    }
}
