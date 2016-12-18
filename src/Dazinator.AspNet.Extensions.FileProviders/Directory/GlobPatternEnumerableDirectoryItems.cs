using DotNet.Globbing;
using System.Collections;
using System.Collections.Generic;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{

    public class GlobPatternEnumerableDirectoryItems : IEnumerable<IDirectoryItem>
    {
        private readonly IFolderDirectoryItem _rootFolder;
        private readonly string _pattern;

        public GlobPatternEnumerableDirectoryItems(IFolderDirectoryItem rootFolder, string pattern)
        {
            _rootFolder = rootFolder;
            _pattern = pattern;
        }

        public IEnumerator<IDirectoryItem> GetEnumerator()
        {

            var folders = new Stack<IFolderDirectoryItem>();
            var glob = Glob.Parse(_pattern);
            var currentFolder = _rootFolder;

            while (currentFolder != null)
            {
                foreach (var item in currentFolder)
                {

                    var isMatch = glob.IsMatch(item.Path);
                    if (isMatch)
                    {
                        yield return item;
                    }

                    if (item.IsFolder)
                    {
                        // add the nested folder to a queue for later processing its items in the loop.
                        var folderItem = (IFolderDirectoryItem)item;
                        folders.Push(folderItem);
                        continue;
                    }

                }

                if (folders.Count > 0)
                {
                    currentFolder = folders.Pop();
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