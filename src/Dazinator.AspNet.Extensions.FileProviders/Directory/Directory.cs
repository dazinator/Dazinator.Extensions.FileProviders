using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public class InMemoryDirectory : IDirectory
    {

        private readonly IFolderDirectoryItem _rootFolder;

        public InMemoryDirectory() : this(new FolderDirectoryItem(string.Empty, null))
        {
        }

        public InMemoryDirectory(IFolderDirectoryItem rootFolder)
        {
            _rootFolder = rootFolder;
        }

        public IFolderDirectoryItem GetFolder(string path)
        {
            IDirectoryItem result = GetItem(path);
            if (result != null)
            {
                if (result.IsFolder)
                {
                    var item = ((IFolderDirectoryItem)result);
                    return item;
                }
            }

            return null;
        }

        public IFolderDirectoryItem GetOrAddFolder(string directory)
        {

            if (string.IsNullOrWhiteSpace(directory))
            {
                return _rootFolder;
            }

            var segments = PathUtils.SplitPathIntoSegments(directory);

            // navigate to root dir.
            //IFolderDirectoryItem topLevelFolder = _rootFolder;
            //while (topLevelFolder.ParentFolder != null)
            //{
            //    topLevelFolder = topLevelFolder.ParentFolder;
            //}

            IFolderDirectoryItem currentFolder = _rootFolder;
            foreach (var folderName in segments)
            {
                currentFolder = currentFolder.GetOrAddFolder(folderName);
            }

            return currentFolder;
        }

        public IFolderDirectoryItem Root => _rootFolder;

        public IFileDirectoryItem GetFile(string path)
        {
            IDirectoryItem result = GetItem(path);
            if (result != null)
            {
                if (!result.IsFolder)
                {
                    var fileItem = ((IFileDirectoryItem)result);
                    return fileItem;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns an item from the directory based on its path, if it exists.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public IDirectoryItem GetItem(string path)
        {
            //IDirectoryItem result = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                // result = this._rootFolder; // return root folder if path is null or empty.
                return this._rootFolder;
            }

            var segments = PathUtils.SplitPathIntoSegments(path);
            IDirectoryItem currentDirectoryItem = _rootFolder;

            foreach (var segment in segments)
            {
                currentDirectoryItem = currentDirectoryItem.GetChildDirectoryItem(segment);

                if (currentDirectoryItem == null)
                {
                    // the item doesn't exist in the directory.
                    return null;
                }

            }

            return currentDirectoryItem;
        }

        /// <summary>
        /// returns all items in the directory that match the specified glob pattern.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <returns></returns>
        public IEnumerable<IDirectoryItem> Search(string globPattern)
        {
            // see https://github.com/kthompson/csharp-glob/issues/2
            var modifiedPattern = globPattern;
            //if (!globPattern.StartsWith("/") || !globPattern.StartsWith("\\"))
            //{
            //    modifiedPattern = "/" + modifiedPattern;
            //}
            var results = new GlobPatternEnumerableDirectoryItems(_rootFolder, modifiedPattern);
            return results;
        }

        public IFileDirectoryItem AddFile(string folderPath, IFileInfo file)
        {
            var folder = GetOrAddFolder(folderPath);
            var result = folder.AddFile(file);
            return result;
        }

        public void UpdateFile(string folderPath, IFileInfo fileInfo)
        {
            IFolderDirectoryItem folder = GetFolder(folderPath);
            folder?.UpdateFile(fileInfo);
        }


    }
}