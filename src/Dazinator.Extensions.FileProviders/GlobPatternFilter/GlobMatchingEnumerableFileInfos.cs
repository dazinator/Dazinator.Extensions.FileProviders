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

            // remove starting slash from any patterns.
        }

        public IEnumerator<Tuple<string, IFileInfo>> GetEnumerator()
        {
            string folderPath = _rootDir.Value.Substring(1) ?? string.Empty; // remove starting "/";
            var currentFolder = _fileProvider.GetDirectoryContents(folderPath);
            Stack<KeyValuePair<string, IFileInfo>> folders = null;

            while (currentFolder != null)
            {
                // loop through all items in the current directory,
                // if we encounter sub folders and we are set to recurse into them,
                // then add them to the stack for later processing.
                foreach (var item in currentFolder)
                {
                    var itemPath = folderPath.Length == 0 ? item.Name : $"{folderPath}/{item.Name}";

                    if (item.IsDirectory)
                    {
                        if (_recurseDirectories)
                        {
                            // we are recursing all directories looking for FILES ONLY.
                            // add the nested folder to a queue for later processing its items in the loop.
                            if (folders == null)
                            {
                                // lazy allocate the list only if recursing,
                                // saves an allocation when not recursing.
                                folders = new Stack<KeyValuePair<string, IFileInfo>>();
                            }
                            folders.Push(new KeyValuePair<string, IFileInfo>(itemPath, item));
                            continue; // don't yield directories we are looking for FILES ONLY.
                        }
                        // In this case, we are reporting top level folder contents only, and we want to output ALL DIRECTORIES by default, as glob patterns dont filter out directories
                        yield return new Tuple<string, IFileInfo>(folderPath, item); // return this directory
                        continue;
                    }

                    //_rootDir.em string.IsNullOrEmpty(folderPath) $"{folderPath}/{item.Name}";
                    bool isMatch = _evaluator.IsAllowed($"/{itemPath}");
                    if (isMatch)
                    {
                        yield return new Tuple<string, IFileInfo>(folderPath, item);
                    }
                }

                // if we have subdirectories to process, pop the next one and set it as current
                // to continue the loop.
                if (_recurseDirectories && folders.Count > 0)
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
