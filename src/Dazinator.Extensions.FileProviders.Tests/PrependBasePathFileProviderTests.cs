using System;
using Xunit;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Threading;
using Dazinator.Extensions.FileProviders.PrependBasePath;

namespace Dazinator.Extensions.FileProviders.Tests
{
    public class PrependBasePathFileProviderTests
    {
        /// <summary>
        /// Tests that the request path provider will forward requests for directory contents starting with
        /// a matching requets path, to an underlying file provider, removing request path information from the path.
        /// </summary>
        [Fact]
        public void Can_Get_Directory_Contents()
        {
            // Arrange


            var dir = System.IO.Directory.GetCurrentDirectory();
            var physicalFileProvider = new PhysicalFileProvider(dir);

            var sut = new PrependBasePathFileProvider("/somepath", physicalFileProvider);

            // Act
            var files = sut.GetDirectoryContents("/somepath/TestDir/");

            // Assert
            bool subDirectoryFound = false;
            bool fileFound = false;

            foreach (var file in files)
            {
                if (file.IsDirectory == true)
                {
                    subDirectoryFound = true;
                    Assert.Equal(file.Name, "AnotherFolder");
                    continue;
                }

                if (file.Name == "TestFile.txt")
                {
                    fileFound = true;
                }
            }

            Assert.True(subDirectoryFound);
            Assert.True(fileFound);

        }

        /// <summary>
        /// Tests that the request path provider will handle requesting directory contents for subpaths that don't
        /// start with a leading slash
        /// </summary>
        [Fact]
        public void Can_Get_Directory_Contents_With_No_Leading_Slash()
        {
            // Arrange


            var dir = System.IO.Directory.GetCurrentDirectory();
            var physicalFileProvider = new PhysicalFileProvider(dir);

            var sut = new PrependBasePathFileProvider("/somepath", physicalFileProvider);

            // Act
            var files = sut.GetDirectoryContents("somepath/TestDir/");

            // Assert
            bool subDirectoryFound = false;
            bool fileFound = false;

            foreach (var file in files)
            {
                if (file.IsDirectory == true)
                {
                    subDirectoryFound = true;
                    Assert.Equal(file.Name, "AnotherFolder");
                    continue;
                }

                if (file.Name == "TestFile.txt")
                {
                    fileFound = true;
                }
            }

            Assert.True(subDirectoryFound);
            Assert.True(fileFound);

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
            var dir = System.IO.Directory.GetCurrentDirectory();
            var physicalFileProvider = new PhysicalFileProvider(dir);
            var sut = new PrependBasePathFileProvider("/somepath", physicalFileProvider);

            var tempFileName = "TestFile" + DateTime.Now.Ticks + ".txt";

            var filePhysicalFilePath = dir + "\\TestDir\\" + "TestFile.txt";
            var tempFilePhysicalFilePath = dir + "\\TestDir\\" + tempFileName;
            File.Copy(filePhysicalFilePath, tempFilePhysicalFilePath);

            // act
            var token = sut.Watch("/somepath/TestDir/" + tempFileName);

            var afterFileChangeEvent = new ManualResetEvent(false);
            token.RegisterChangeCallback((a) =>
            {
                afterFileChangeEvent.Set();
                // changeFired = true;
            }, null);

            // Modify the file. Should trigger the change token callback as we are watching the file.
            using (StreamWriter sw = File.AppendText(tempFilePhysicalFilePath))
            {
                sw.WriteLine("Changed");
                sw.Flush();
                // sw.Close();
            }

            // Assert
            afterFileChangeEvent.WaitOne(new TimeSpan(0, 0, 1));

            File.Delete(tempFilePhysicalFilePath);

        }

        [Fact]
        public void Can_Watch_Multiple_Files_For_Changes()
        {
            // arrange
            var dir = System.IO.Directory.GetCurrentDirectory();
            var physicalFileProvider = new PhysicalFileProvider(dir);
            var sut = new PrependBasePathFileProvider("/somepath", physicalFileProvider);

            var tempFileName = "TestFile" + DateTime.Now.Ticks + ".txt";

            var filePhysicalFilePath = dir + "\\TestDir\\" + "TestFile.txt";
            var tempFilePhysicalFilePath = dir + "\\TestDir\\" + tempFileName;
            File.Copy(filePhysicalFilePath, tempFilePhysicalFilePath);

            var anotherFilePhysicalFilePath = dir + "\\TestDir\\AnotherFolder\\" + tempFileName;
            File.Copy(filePhysicalFilePath, anotherFilePhysicalFilePath);


            // act
            var token = sut.Watch("/somepath/**/*.*" + tempFileName);

            var afterFileChangeEvent = new ManualResetEvent(false);
            token.RegisterChangeCallback((a) =>
            {
                afterFileChangeEvent.Set();
                // changeFired = true;
            }, null);

            // Modify the first file. Should trigger the change token callback as we are watching the file.
            using (StreamWriter sw = File.AppendText(tempFilePhysicalFilePath))
            {
                sw.WriteLine("Changed");
                sw.Flush();
                // sw.Close();
            }

            // Assert
            var timeout = new TimeSpan(0, 0, 1);
            afterFileChangeEvent.WaitOne(timeout);

            afterFileChangeEvent.Reset();

            // Modify the first file. Should trigger the change token callback as we are watching the file.
            using (StreamWriter sw = File.AppendText(anotherFilePhysicalFilePath))
            {
                sw.WriteLine("Changed");
                sw.Flush();
                // sw.Close();
            }

            afterFileChangeEvent.WaitOne(timeout);



            File.Delete(tempFilePhysicalFilePath);
            File.Delete(anotherFilePhysicalFilePath);
        }




    }
}
