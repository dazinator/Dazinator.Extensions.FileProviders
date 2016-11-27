using Dazinator.AspNet.Extensions.FileProviders.Globbing;
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
        private readonly string _pattern;
     
        public GlobMatchingEnumerableFileInfos(IFileProvider fileProvider, string pattern)
        {
            _fileProvider = fileProvider;
            _pattern = pattern;
        }

        public IEnumerator<Tuple<string, IFileInfo>> GetEnumerator()
        {

            var folders = new Stack<KeyValuePair<string, IFileInfo>>();
            var glob = new Glob(_pattern);

            var folderPath = "";
            var currentFolder = _fileProvider.GetDirectoryContents("");
            
            while (currentFolder != null)
            {
                // var basePath = currentFolder.Name
                foreach (var item in currentFolder)
                {
                    var itemPath = $"{folderPath}/{item.Name}";
                    var isMatch = glob.IsMatch(itemPath);
                    if (isMatch)
                    {
                        yield return new Tuple<string, IFileInfo>(folderPath, item);
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
