using System;
using Xunit;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Threading;
using Dazinator.Extensions.FileProviders.PrependBasePath;
using System.Linq;
using Dazinator.Extensions.FileProviders.InMemory;

namespace Dazinator.Extensions.FileProviders.Tests
{
    public class PrependBasePathFileProviderTests
    {
        /// <summary>
        /// Tests that the request path provider will forward requests for directory contents starting with
        /// a matching requets path, to an underlying file provider, removing request path information from the path.
        /// </summary>
        [Theory]
        [InlineData("", "/somepath", "/somepath/TestDir/", "/TestDir")]
        [InlineData("TestDir", "/foo", "/foo/AnotherFolder", "/AnotherFolder")]
        [InlineData("TestDir", "/foo", "foo/AnotherFolder", "/AnotherFolder")] // no leading slash in request path

        public void Can_Get_Directory_Contents(string rootFolderDir, string prependBasePath, string requestPath, string physicalPathForCompare)
        {
            // Arrange


            var dir = Directory.GetCurrentDirectory();
            var rootDir = Path.Combine(dir, rootFolderDir);
            var physicalFileProvider = new PhysicalFileProvider(rootDir);

            var sut = new PrependBasePathFileProvider(prependBasePath, physicalFileProvider);

            // Act
            var files = sut.GetDirectoryContents(requestPath);
            var fileList = files.ToList();
            // Assert
            var expectedFiles = physicalFileProvider.GetDirectoryContents(physicalPathForCompare);
            var expectedFileNames = expectedFiles.ToList().Select(a => a.Name).ToArray();
            Assert.Equal(expectedFileNames.Length, fileList.Count);

            foreach (var file in fileList)
            {
                Assert.Contains(file.Name, expectedFileNames);
            }
        }

        /// <summary>
        /// Tests that the request path provider will forward requests for directory contents starting with
        /// a matching requets path, to an underlying file provider, removing request path information from the path.
        /// </summary>
        [Fact]
        public void Can_Get_Root_Directory_Contents()
        {
            // Arrange


            var dir = System.IO.Directory.GetCurrentDirectory();
            var physicalFileProvider = new PhysicalFileProvider(dir);

            var sut = new PrependBasePathFileProvider("/somepath", physicalFileProvider);

            // Act
            var files = sut.GetDirectoryContents("");

            // Assert
            bool rootDirectoryFound = false;

            foreach (var file in files)
            {
                if (file.IsDirectory == true)
                {
                    rootDirectoryFound = true;
                    Assert.Equal(file.Name, "somepath");
                    continue;
                }
            }

            Assert.True(rootDirectoryFound);
        }

        [Fact]
        public void Can_Watch_A_Single_File_For_Changes()
        {
            // arrange
            var inMemoryFileProvider = new InMemoryFileProvider();
            var tempFileName = "TestFile" + DateTime.Now.Ticks + ".txt";

            inMemoryFileProvider.Directory.AddFile("/TestDir", new StringFileInfo("foo", tempFileName));
            var sut = new PrependBasePathFileProvider("/somepath", inMemoryFileProvider);

            //var filePhysicalFilePath = dir + "\\TestDir\\" + "TestFile.txt";
            //var tempFilePhysicalFilePath = dir + "\\TestDir\\" + tempFileName;
            //File.Copy(filePhysicalFilePath, tempFilePhysicalFilePath);

            // act
            var token = sut.Watch("/somepath/TestDir/" + tempFileName);

            var afterFileChangeEvent = new ManualResetEvent(false);
            token.RegisterChangeCallback((a) =>
            {
                afterFileChangeEvent.Set();
                // changeFired = true;
            }, null);

            // Modify the file. Should trigger the change token callback as we are watching the file.
            inMemoryFileProvider.Directory.AddOrUpdateFile("/TestDir", new StringFileInfo("bar", tempFileName));
            // Assert
            afterFileChangeEvent.WaitOne(new TimeSpan(0, 0, 1));

            // File.Delete(tempFilePhysicalFilePath);

        }

        [Fact]
        public void Can_Watch_Multiple_Files_For_Changes()
        {
            // arrange
            var inMemoryFileProvider = new InMemoryFileProvider();


            //  var dir = System.IO.Directory.GetCurrentDirectory();
            //  var physicalFileProvider = new PhysicalFileProvider(dir);
            var sut = new PrependBasePathFileProvider("/somepath", inMemoryFileProvider);

            var tempFileName = "TestFile" + DateTime.Now.Ticks + ".txt";
            inMemoryFileProvider.Directory.AddFile("/TestDir", new StringFileInfo("foo", tempFileName));
            inMemoryFileProvider.Directory.AddFile("/TestDir/AnotherFolder", new StringFileInfo("foo", tempFileName));

            // act
            var token = sut.Watch("/somepath/**/*.*" + tempFileName);

            var afterFileChangeEvent = new ManualResetEvent(false);
            token.RegisterChangeCallback((a) =>
            {
                afterFileChangeEvent.Set();
                // changeFired = true;
            }, null);

            // Modify the first file. Should trigger the change token callback as we are watching the file.
            inMemoryFileProvider.Directory.AddOrUpdateFile("/TestDir/AnotherFolder", new StringFileInfo("bar", tempFileName));

            // Assert
            var timeout = new TimeSpan(0, 0, 1);
            afterFileChangeEvent.WaitOne(timeout);

            afterFileChangeEvent.Reset();

            // Modify the first file. Should trigger the change token callback as we are watching the file.
            inMemoryFileProvider.Directory.AddOrUpdateFile("/TestDir", new StringFileInfo("bar", tempFileName));


            afterFileChangeEvent.WaitOne(timeout);
        }
    }
}
