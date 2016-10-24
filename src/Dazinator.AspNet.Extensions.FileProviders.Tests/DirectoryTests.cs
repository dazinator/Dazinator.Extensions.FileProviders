using System;
using Xunit;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Threading;
using Dazinator.AspNet.Extensions.FileProviders.Directory;
using Dazinator.AspNet.Extensions.FileProviders;
using NuGet.Common;
using System.Linq;

namespace FileProvider.Tests
{
    public class DirectoryTests
    {
        /// <summary>
        /// Tests for the InMemoryDirectory sub system.
        /// </summary>
        [Fact]
        public void Can_Add_Then_Retrieve_A_File()
        {
            // Arrange

            IDirectory directory = new InMemoryDirectory();
            var fileContents = "greetings!";
            IFileInfo fileInfo = new StringFileInfo(fileContents, "hello.txt");

            // Act
            // Add the file to the directory, to the folder path: /foo/bar
            directory.AddFile("/foo", fileInfo);

            // Assert
            // Assert that we can get the file from the directory using its full path.
            var retrievedFile = directory.GetFile("/foo/hello.txt");
            Assert.NotNull(retrievedFile);
            Assert.Equal(fileInfo, retrievedFile.FileInfo);

        }

        [Fact]
        public void Can_Add_Then_Retrieve_A_Folder()
        {
            // Arrange

            IDirectory directory = new InMemoryDirectory();
            // Act
            // Add the file to the directory, to the folder path: /foo/bar
            var folder = directory.GetOrAddFolder("/folder");
            Assert.NotNull(folder);

            var retrievedFolder = directory.GetFolder("/folder");
            Assert.NotNull(retrievedFolder);
            Assert.Equal(folder, retrievedFolder);
        }

        [Fact]
        public void Can_Add_Folder_Directory_Structure()
        {
            // Arrange
            IDirectory directory = new InMemoryDirectory();

            // Act
            // Adds a folder to the directory, along with the necessary directory structure
            var folder = directory.GetOrAddFolder("/some/dir/folder");
            Assert.NotNull(folder);

            var parentFolder = directory.GetFolder("/some/dir");
            var grandparentFolder = directory.GetFolder("/some");

            // "/some/dir/folder" should have parent folder of "/some/dir"
            Assert.Equal(parentFolder, folder.ParentFolder);
            Assert.Equal("/some/dir/folder", folder.Path);
            Assert.Equal("folder", folder.Name);
            Assert.NotNull(folder.FileInfo);
            Assert.True(folder.IsFolder);

            // "/some/dir/" should have parent folder of "/some"
            Assert.Equal(grandparentFolder, parentFolder.ParentFolder);
            Assert.Equal("/some/dir", parentFolder.Path);
            Assert.Equal("dir", parentFolder.Name);
            Assert.NotNull(parentFolder.FileInfo);
            Assert.True(parentFolder.IsFolder);

            // "/some" should have parent folder which is the root folder for this directory.
            Assert.Equal(directory.Root, grandparentFolder.ParentFolder);
            Assert.Equal("/some", grandparentFolder.Path);
            Assert.Equal("some", grandparentFolder.Name);
            Assert.NotNull(grandparentFolder.FileInfo);
            Assert.True(grandparentFolder.IsFolder);

        }


        [Theory]
        [InlineData("/some/dir/folder/file.txt|/some/dir/folder/file.csv", "/some/dir/folder/file.*", 2)]
        [InlineData("/file.txt|/folder/file.csv", "/*file.txt", 1)]
        [InlineData("/file.txt|/folder/file.csv", "*file.csv", 1)]
        [InlineData("/file.txt|/folder/file.csv", "*file.*", 2)]
        public void Can_Search_Directory(string files, string pattern, int expectedMatchCount)
        {
            // Arrange
            IDirectory directory = BuildDirectoryWithTestFiles(files);
            var results = directory.Search(pattern).ToList();
            Assert.Equal(expectedMatchCount, results.Count);
        }


        private IDirectory BuildDirectoryWithTestFiles(string filesInformation)
        {
            var directory = new InMemoryDirectory();
            var filesArray = filesInformation.Split('|');
            foreach (var file in filesArray)
            {

                var pathSegments = PathUtils.SplitPathIntoSegments(file);

                var fileName = pathSegments[pathSegments.Length - 1];
                string dir = string.Empty;

                if (pathSegments.Length > 1)
                {
                    for (int i = 0; i < pathSegments.Length - 1; i++)
                    {
                        dir = dir + "/" + pathSegments[i];
                    }
                }

                var fileContents = Guid.NewGuid();
                IFileInfo fileInfo = new StringFileInfo(fileContents.ToString(), fileName);
                directory.AddFile(dir, fileInfo);
            }

            return directory;
        }

    }
}
