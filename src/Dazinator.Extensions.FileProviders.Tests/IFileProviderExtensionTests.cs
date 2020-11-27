using Dazinator.Extensions.FileProviders.InMemory;
using Dazinator.Extensions.FileProviders.InMemory.Directory;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Dazinator.Extensions.FileProviders.Tests
{
    public class IFileProviderExtensionTests
    {
        /// <summary>
        /// Tests for the InMemoryDirectory sub system.
        /// </summary>
        [Fact]
        public void Can_Search_For_Files()
        {
            // Arrange

            var directory = new InMemoryDirectory();
           
            // Add some files
            directory.AddFile("/foo", new StringFileInfo("greetings", "bar.txt"));
            directory.AddFile("/foo", new StringFileInfo("another", "baz.txt"));
            directory.AddFile("/bar", new StringFileInfo("hi", "bar.txt"));
            directory.AddFile("/foo", new StringFileInfo("foo", "bar.txt.min"));

            var testProvider = new InMemoryFileProvider(directory);

            // Act


            // Assert
            // Assert that we can get the file from the directory using its full path.
            var results = testProvider.Search("/foo/*.txt").ToArray();
            Assert.NotNull(results);
            Assert.Equal(2, results.Length);

            var fileA = results[0];
            var fileB = results[1];

            var expectedFileNames = new Dictionary<string, string> { { "bar.txt", "greetings" }, { "baz.txt", "another" } };

            Assert.Equal("/foo", fileA.Item1);
            Assert.Contains(fileA.Item2.Name, expectedFileNames.Keys);
            Assert.Equal(expectedFileNames[fileA.Item2.Name], fileA.Item2.ReadAllContent());

            Assert.Equal("/foo", fileB.Item1);
            Assert.Contains(fileB.Item2.Name, expectedFileNames.Keys);
            Assert.Equal(expectedFileNames[fileB.Item2.Name], fileB.Item2.ReadAllContent());

        }

        [Fact]
        public void Can_Search_For_Files_Using_Multiple_Includes()
        {
            // Arrange

            var directory = new InMemoryDirectory();

            // Add some files
            directory.AddFile("/foo", new StringFileInfo("greetings", "bar.txt"));
            directory.AddFile("/foo", new StringFileInfo("another", "baz.txt"));
            directory.AddFile("/bar", new StringFileInfo("hi", "bar.txt"));
            directory.AddFile("/foo", new StringFileInfo("foo", "bar.txt.min"));

            var testProvider = new InMemoryFileProvider(directory);

            // Act


            // Assert
            // Assert that we can get the file from the directory using its full path.
            var results = testProvider.Search("/foo/*.txt", "/bar/bar.txt").ToArray();
            Assert.NotNull(results);
            Assert.Equal(3, results.Length);
          
        }

        [Fact]
        public void Can_Search_For_Files_Using_Multiple_Includes_And_Excludes()
        {
            // Arrange

            var directory = new InMemoryDirectory();

            // Add some files
            directory.AddFile("/foo", new StringFileInfo("greetings", "bar.txt"));
            directory.AddFile("/foo", new StringFileInfo("another", "baz.txt"));
            directory.AddFile("/bar", new StringFileInfo("hi", "bar.txt"));
            directory.AddFile("/foo", new StringFileInfo("foo", "bar.txt.min"));

            var testProvider = new InMemoryFileProvider(directory);

            // Act


            // Assert
            // Assert that we can get the file from the directory using its full path.
            var includes = new string[] { "/foo/*.txt", "/bar/bar.txt" };

            var results = testProvider.Search(includes, "/foo/baz.txt").ToArray();
            Assert.NotNull(results);
            Assert.Equal(2, results.Length);

        }





    }
}
