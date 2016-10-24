using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public class Directory : IDirectory
    {

        private readonly IFolderDirectoryItem _rootFolder;

        public Directory() : this(new FolderDirectoryItem(string.Empty, null))
        {
        }

        public Directory(IFolderDirectoryItem rootFolder)
        {
            _rootFolder = rootFolder;
        }

        public IFolderDirectoryItem GetOrAddDirectory(string directory)
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

        public bool TryGetFile(string path, out IFileDirectoryItem fileItem)
        {
            IDirectoryItem result;
            if (TryGetItem(path, out result))
            {
                if (!result.IsFolder)
                {
                    fileItem = ((IFileDirectoryItem)result);
                    return true;
                }
            }

            fileItem = null;
            return false;
        }

        public bool TryGetFolder(string path, out IFolderDirectoryItem fileItem)
        {
            IDirectoryItem result;
            if (TryGetItem(path, out result))
            {
                if (result.IsFolder)
                {
                    fileItem = ((IFolderDirectoryItem)result);
                    return true;
                }
            }

            fileItem = null;
            return false;
        }

        /// <summary>
        /// Returns an item from the directory based on its path, if it exists.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryGetItem(string path, out IDirectoryItem item)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                item = this._rootFolder; // return root folder if path is null or empty.
                return true;
            }

            var segments = PathUtils.SplitPathIntoSegments(path);
            IDirectoryItem currentDirectoryItem = _rootFolder;

            foreach (var segment in segments)
            {
                currentDirectoryItem = currentDirectoryItem.GetChildDirectoryItem(segment);

                if (currentDirectoryItem == null)
                {
                    // the item doesn't exist in the directory.
                    item = null;
                    return false;
                }

            }

            item = currentDirectoryItem;
            return true;

        }

        /// <summary>
        /// returns all items in the directory that match the specified glob pattern.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <returns></returns>
        public IEnumerable<IDirectoryItem> Search(string globPattern)
        {
            var results = new GlobPatternEnumerableDirectoryItems(_rootFolder, globPattern);
            return results;
        }

        public IFileDirectoryItem AddFile(string folderPath, IFileInfo file)
        {
            var folder = GetOrAddDirectory(folderPath);
            var result = folder.AddFile(file);
            return result;
        }

        public void UpdateFile(string folderPath, IFileInfo fileInfo)
        {
            //var existingFile = GetFileInfo(path);
            IFolderDirectoryItem folder;
            if (TryGetFolder(folderPath, out folder))
            {
                folder.UpdateFile(fileInfo);
            }

            //AddFile()


            // Files[path] = stringFileInfo;

            // IChangeToken fileToken;
            // if (_matchInfoCache.TryGetValue(path, out fileToken))
            // {
            //     var inMemory = fileToken as InMemoryChangeToken;
            //     if (inMemory != null)
            //     {
            //         // return existing token for this file path.
            //         inMemory.HasChanged = true;
            //         inMemory.RaiseCallback(stringFileInfo);
            //     }
            // }
        }


    }
}