using System;
using Dazinator.AspNet.Extensions.FileProviders.Directory;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Dazinator.AspNet.Extensions.FileProviders;

namespace FileProvider.Tests
{
    public class DirectoryWatcherTests
    {


        public DirectoryWatcherTests()
        {
            

        }


        [Theory]
        [InlineData("/some/dir/folder/file.txt|/some/dir/folder/file.csv", "/some/dir/folder/file.*", 2)]
        [InlineData("/file.txt|/folder/file.csv", "/*file.txt", 1)]
        [InlineData("/file.txt|/folder/file.csv", "*file.csv", 1)]
        [InlineData("/file.txt|/folder/file.csv", "*file.*", 2)]
        [Fact]
        public void Can_Watch_Files_For_Changes(string files, string pattern, int expectedMatchCount)
        {
            // Arrange
            IDirectory directory = BuildDirectoryWithTestFiles(files);
            var watcher = new DirectoryWatcher(directory);
            watcher.Watch(pattern);
            
            



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
