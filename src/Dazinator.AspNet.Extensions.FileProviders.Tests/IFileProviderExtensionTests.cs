using Dazinator.AspNet.Extensions.FileProviders;
using Dazinator.AspNet.Extensions.FileProviders.Directory;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FileProvider.Tests
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
            Assert.Equal(results[0].Item1, "/foo");
            Assert.Equal(results[0].Item2.Name, "bar.txt");
            Assert.Equal(results[1].Item1, "/foo");
            Assert.Equal(results[1].Item2.Name, "baz.txt");

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
