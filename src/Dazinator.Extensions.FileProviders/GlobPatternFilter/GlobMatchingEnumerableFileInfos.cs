using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Dazinator.Extensions.FileProviders.GlobPatternFilter
{
    public class GlobMatchingEnumerableFileInfos : IEnumerable<Tuple<string, IFileInfo>>
    {
        private readonly PathString _rootDir;
        private readonly bool _recurseDirectories;
        private readonly IFileProvider _fileProvider;
        private readonly GlobPatternIncludeExcludeEvaluator _evaluator;

        public GlobMatchingEnumerableFileInfos(
            PathString rootDir,
            bool recurseDirectories,
            IFileProvider fileProvider,
            params string[] includePatterns) : this(rootDir, recurseDirectories, fileProvider, includePatterns, null)
        {
        }

        public GlobMatchingEnumerableFileInfos(PathString rootDir,
            bool recurseDirectories,
            IFileProvider fileProvider,
            string[] includePatterns,
            params string[] excludePatterns) : this(rootDir, recurseDirectories, fileProvider, new GlobPatternIncludeExcludeEvaluator(includePatterns, excludePatterns))
        {
        }

        public GlobMatchingEnumerableFileInfos(PathString rootDir,
           bool recurseDirectories,
           IFileProvider fileProvider,
           GlobPatternIncludeExcludeEvaluator evaluator)
        {
            _rootDir = rootDir.HasValue ? rootDir : new PathString("/");
            _recurseDirectories = recurseDirectories;
            _fileProvider = fileProvider;
            _evaluator = evaluator;
        }

        public IEnumerator<Tuple<string, IFileInfo>> GetEnumerator()
        {
            var folderPath = _rootDir;
            var currentFolder = _fileProvider.GetDirectoryContents(_rootDir);
            Stack<KeyValuePair<string, IFileInfo>> folders = null;


            while (currentFolder != null)
            {
                // loop through all items in the current directory,
                // if we encounter sub folders and we are set to recurse into them,
                // then add them to the stack for later processing.
                foreach (var item in currentFolder)
                {
                    var itemPath = folderPath.Value.Length == 1 ? item.Name : (folderPath.Add($"/{item.Name}").ToString());

                    //_rootDir.em string.IsNullOrEmpty(folderPath) $"{folderPath}/{item.Name}";
                    bool isMatch = _evaluator.IsAllowed(itemPath);

                    if (isMatch)
                    {
                        yield return new Tuple<string, IFileInfo>(folderPath, item);
                    }

                    if (item.IsDirectory && _recurseDirectories)
                    {
                        // add the nested folder to a queue for later processing its items in the loop.
                        if (folders == null)
                        {
                            // lazy allocate the list only if recursing,
                            // saves an allocation when not recursing.
                            folders = new Stack<KeyValuePair<string, IFileInfo>>();
                        }
                        folders.Push(new KeyValuePair<string, IFileInfo>(itemPath, item));
                        continue;
                    }
                }

                // if we have subdirectories to process, pop the next one and set it as current
                // to continue the loop.
                if (_recurseDirectories && folders.Count > 0)
                {
                    var folderItem = folders.Pop();
                    folderPath = $"/{folderItem.Key}";
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
