using Xunit;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Dazinator.Extensions.FileProviders.GlobPatternFilter;
using System.Linq;

namespace Dazinator.Extensions.FileProviders.Tests
{
    public class GlobPatternFilterFileProviderTests
    {
        [Theory]
        [InlineData("", "/TestDir/*.txt", "/TestDir", new string[] { "TestFile.txt" })]
        [InlineData("TestDir/csv", "[bB]a*.csv", "", new string[] { "ba_1_2.csv" })]
        public void Can_Get_Filtered_Directory_Contents(string rootFolderDir, string includeGlob, string getDirectoryContentsPath, string[] expectedFileInfoNames)
        {
            // Arrange
            var dir = System.IO.Directory.GetCurrentDirectory();
            var rootDir = Path.Combine(dir, rootFolderDir);
            var physicalFileProvider = new PhysicalFileProvider(rootDir);

            //  var includeGlob = "/TestDir/*.txt";
            var sut = new GlobPatternFilterFileProvider(physicalFileProvider, new string[] { includeGlob });

            // Act
            var files = sut.GetDirectoryContents(getDirectoryContentsPath);
            Assert.NotEmpty(files);

            var fileList = files.Where(a => !a.IsDirectory).ToList();

            if (fileList.Count > expectedFileInfoNames.Length)
            {
                throw new System.Exception(string.Join(", ", fileList.Select(a => a.Name)));
            }
            Assert.Equal(expectedFileInfoNames.Length, fileList.Count);


            foreach (var file in fileList)
            {
                Assert.Contains(file.Name, expectedFileInfoNames);

                //   Assert.False(file.IsDirectory, $"directory contents filtered by glob expression {includeGlob} should only contain *.txt file, not directories");

                // $"directory contents filtered by glob expression {includeGlob} should only contain *.txt files, not other extensions"
                //   Assert.Equal(".txt", Path.GetExtension(file.Name));
            }

        }


        [Fact]
        public void Can_Get_Filtered_Directory_Contents_Folder_Only()
        {
            // Arrange
            var dir = System.IO.Directory.GetCurrentDirectory();
            var physicalFileProvider = new PhysicalFileProvider(dir);

            var includeGlob = "/TestDir/AnotherFolder";
            var sut = new GlobPatternFilterFileProvider(physicalFileProvider, new string[] { includeGlob });

            // Act
            var files = sut.GetDirectoryContents("/TestDir");
            Assert.NotEmpty(files);

            foreach (var file in files)
            {
                Assert.True(file.IsDirectory, $"directory contents filtered by glob expression {includeGlob} should only contain AnotherFolder, not files.");

            }

        }

        [Fact]
        public void Can_Get_Allowed_Files_From_Allowed_Directory()
        {
            // Arrange
            var dir = System.IO.Directory.GetCurrentDirectory();
            var physicalFileProvider = new PhysicalFileProvider(dir);

            var includeGlob = "/TestDir/AnotherFolder/**";
            var sut = new GlobPatternFilterFileProvider(physicalFileProvider, new string[] { includeGlob });

            // Act
            var file = sut.GetFileInfo("/TestDir/TestFile.txt");
            Assert.False(file.Exists);


            file = sut.GetFileInfo("/TestDir/AnotherFolder/AnotherTestFile.txt");
            Assert.True(file.Exists);

        }

        ///// <summary>
        ///// Tests that the request path provider will handle requesting directory contents for subpaths that don't
        ///// start with a leading slash
        ///// </summary>
        //[Fact]
        //public void Can_Get_Directory_Contents_With_No_Leading_Slash()
        //{
        //    // Arrange


        //    var dir = System.IO.Directory.GetCurrentDirectory();
        //    var physicalFileProvider = new PhysicalFileProvider(dir);

        //    var sut = new RequestPathFileProvider("/somepath", physicalFileProvider);

        //    // Act
        //    var files = sut.GetDirectoryContents("somepath/TestDir/");

        //    // Assert
        //    bool subDirectoryFound = false;
        //    bool fileFound = false;

        //    foreach (var file in files)
        //    {
        //        if (file.IsDirectory == true)
        //        {
        //            subDirectoryFound = true;
        //            Assert.Equal(file.Name, "AnotherFolder");
        //            continue;
        //        }

        //        if (file.Name == "TestFile.txt")
        //        {
        //            fileFound = true;
        //        }
        //    }

        //    Assert.True(subDirectoryFound);
        //    Assert.True(fileFound);

        //}

        ///// <summary>
        ///// Tests that the request path provider will forward requests for directory contents starting with
        ///// a matching requets path, to an underlying file provider, removing request path information from the path.
        ///// </summary>
        //[Fact]
        //public void Can_Get_Root_Directory_Contents()
        //{
        //    // Arrange


        //    var dir = System.IO.Directory.GetCurrentDirectory();
        //    var physicalFileProvider = new PhysicalFileProvider(dir);

        //    var sut = new RequestPathFileProvider("/somepath", physicalFileProvider);

        //    // Act
        //    var files = sut.GetDirectoryContents("");

        //    // Assert
        //    bool rootDirectoryFound = false;

        //    foreach (var file in files)
        //    {
        //        if (file.IsDirectory == true)
        //        {
        //            rootDirectoryFound = true;
        //            Assert.Equal(file.Name, "somepath");
        //            continue;
        //        }
        //    }

        //    Assert.True(rootDirectoryFound);
        //}

        //[Fact]
        //public void Can_Watch_A_Single_File_For_Changes()
        //{
        //    // arrange
        //    var dir = System.IO.Directory.GetCurrentDirectory();
        //    var physicalFileProvider = new PhysicalFileProvider(dir);
        //    var sut = new RequestPathFileProvider("/somepath", physicalFileProvider);

        //    var tempFileName = "TestFile" + DateTime.Now.Ticks + ".txt";

        //    var filePhysicalFilePath = dir + "\\TestDir\\" + "TestFile.txt";
        //    var tempFilePhysicalFilePath = dir + "\\TestDir\\" + tempFileName;
        //    File.Copy(filePhysicalFilePath, tempFilePhysicalFilePath);

        //    // act
        //    var token = sut.Watch("/somepath/TestDir/" + tempFileName);

        //    var afterFileChangeEvent = new ManualResetEvent(false);
        //    token.RegisterChangeCallback((a) =>
        //    {
        //        afterFileChangeEvent.Set();
        //        // changeFired = true;
        //    }, null);

        //    // Modify the file. Should trigger the change token callback as we are watching the file.
        //    using (StreamWriter sw = File.AppendText(tempFilePhysicalFilePath))
        //    {
        //        sw.WriteLine("Changed");
        //        sw.Flush();
        //        // sw.Close();
        //    }

        //    // Assert
        //    afterFileChangeEvent.WaitOne(new TimeSpan(0, 0, 1));

        //    File.Delete(tempFilePhysicalFilePath);

        //}

        //[Fact]
        //public void Can_Watch_Multiple_Files_For_Changes()
        //{
        //    // arrange
        //    var dir = System.IO.Directory.GetCurrentDirectory();
        //    var physicalFileProvider = new PhysicalFileProvider(dir);
        //    var sut = new RequestPathFileProvider("/somepath", physicalFileProvider);

        //    var tempFileName = "TestFile" + DateTime.Now.Ticks + ".txt";

        //    var filePhysicalFilePath = dir + "\\TestDir\\" + "TestFile.txt";
        //    var tempFilePhysicalFilePath = dir + "\\TestDir\\" + tempFileName;
        //    File.Copy(filePhysicalFilePath, tempFilePhysicalFilePath);

        //    var anotherFilePhysicalFilePath = dir + "\\TestDir\\AnotherFolder\\" + tempFileName;
        //    File.Copy(filePhysicalFilePath, anotherFilePhysicalFilePath);


        //    // act
        //    var token = sut.Watch("/somepath/**/*.*" + tempFileName);

        //    var afterFileChangeEvent = new ManualResetEvent(false);
        //    token.RegisterChangeCallback((a) =>
        //    {
        //        afterFileChangeEvent.Set();
        //        // changeFired = true;
        //    }, null);

        //    // Modify the first file. Should trigger the change token callback as we are watching the file.
        //    using (StreamWriter sw = File.AppendText(tempFilePhysicalFilePath))
        //    {
        //        sw.WriteLine("Changed");
        //        sw.Flush();
        //        // sw.Close();
        //    }

        //    // Assert
        //    var timeout = new TimeSpan(0, 0, 1);
        //    afterFileChangeEvent.WaitOne(timeout);

        //    afterFileChangeEvent.Reset();

        //    // Modify the first file. Should trigger the change token callback as we are watching the file.
        //    using (StreamWriter sw = File.AppendText(anotherFilePhysicalFilePath))
        //    {
        //        sw.WriteLine("Changed");
        //        sw.Flush();
        //        // sw.Close();
        //    }

        //    afterFileChangeEvent.WaitOne(timeout);



        //    File.Delete(tempFilePhysicalFilePath);
        //    File.Delete(anotherFilePhysicalFilePath);
        //}




    }
}
