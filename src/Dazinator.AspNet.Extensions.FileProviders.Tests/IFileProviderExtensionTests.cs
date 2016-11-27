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

      
    }
}
