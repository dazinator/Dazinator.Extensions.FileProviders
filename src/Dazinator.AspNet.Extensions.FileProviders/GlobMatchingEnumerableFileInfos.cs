using DotNet.Globbing;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    public class GlobMatchingEnumerableFileInfos : IEnumerable<Tuple<string, IFileInfo>>
    {
        private readonly IFileProvider _fileProvider;
        private readonly string[] _includePatterns;
        private readonly string[] _excludePatterns;


        public GlobMatchingEnumerableFileInfos(IFileProvider fileProvider, params string[] includePatterns) : this(fileProvider, includePatterns, null)
        {

            // _isExclude = isExclude;
        }

        public GlobMatchingEnumerableFileInfos(IFileProvider fileProvider, string[] includePatterns, params string[] excludePatterns)
        {
            _fileProvider = fileProvider;
            _includePatterns = includePatterns;
            _excludePatterns = excludePatterns;
            // _isExclude = isExclude;
        }
        public IEnumerator<Tuple<string, IFileInfo>> GetEnumerator()
        {

            var includeGlobs = new List<Glob>(_includePatterns.Length);
            var excludeGlobs = new List<Glob>(_excludePatterns == null ? 0 : _excludePatterns.Length);

            var folders = new Stack<KeyValuePair<string, IFileInfo>>();

            foreach (var pattern in _includePatterns)
            {
                var glob = Glob.Parse(pattern);
                includeGlobs.Add(glob);
            }

            if (_excludePatterns != null)
            {
                foreach (var pattern in _excludePatterns)
                {
                    var glob = Glob.Parse(pattern);
                    excludeGlobs.Add(glob);
                }
            }

            var folderPath = "";
            var currentFolder = _fileProvider.GetDirectoryContents("");

            while (currentFolder != null)
            {
                // var basePath = currentFolder.Name
                foreach (var item in currentFolder)
                {
                    var itemPath = $"{folderPath}/{item.Name}";
                    bool isMatch = false;

                    foreach (var glob in includeGlobs)
                    {
                        isMatch = glob.IsMatch(itemPath);
                        if (isMatch)
                        {
                            break;
                        }
                    }

                    if (isMatch)
                    {
                        bool isExcluded = false;
                        foreach (var glob in excludeGlobs)
                        {
                            isExcluded = glob.IsMatch(itemPath);
                            if (isExcluded)
                            {
                                // exclude file.
                                break;
                            }
                        }
                        if (!isExcluded)
                        {
                            yield return new Tuple<string, IFileInfo>(folderPath, item);
                        }
                    }

                    if (item.IsDirectory)
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
                    // finished iteating entire directory.
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
