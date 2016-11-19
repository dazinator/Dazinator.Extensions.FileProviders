using Xunit;
using Microsoft.Extensions.FileProviders;
using Dazinator.AspNet.Extensions.FileProviders;
using Microsoft.AspNetCore.Testing.xunit;
using Dazinator.AspNet.Extensions.FileProviders.Directory;
using System.IO;
using System;

namespace FileProvider.Tests
{
    public class InMemoryFileProviderTests
    {

        [Fact]
        public void GetFileInfo_ReturnsNotFoundFileInfo_ForNullPath()
        {
            using (var provider = new InMemoryFileProvider())
            {
                var info = provider.GetFileInfo(null);
                Assert.IsType(typeof(NotFoundFileInfo), info);
            }
        }

        [Fact]
        public void GetFileInfo_ReturnsNotFoundFileInfo_ForEmptyPath()
        {
            using (var provider = new InMemoryFileProvider())
            {
                var info = provider.GetFileInfo(string.Empty);
                Assert.IsType(typeof(NotFoundFileInfo), info);
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [InlineData("/")]
        [InlineData("///")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        public void GetFileInfo_ReturnsNotFoundFileInfo_ForValidPathsWithLeadingSlashes_Windows(string path)
        {
            GetFileInfo_ReturnsNotFoundFileInfo_ForValidPathsWithLeadingSlashes(path);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Testing Unix specific behaviour on leading slashes.")]
        [InlineData("/")]
        [InlineData("///")]
        public void GetFileInfo_ReturnsNotFoundFileInfo_ForValidPathsWithLeadingSlashes_Unix(string path)
        {
            GetFileInfo_ReturnsNotFoundFileInfo_ForValidPathsWithLeadingSlashes(path);
        }

        public void GetFileInfo_ReturnsNotFoundFileInfo_ForValidPathsWithLeadingSlashes(string path)
        {
            using (var provider = new InMemoryFileProvider())
            {
                var info = provider.GetFileInfo(path);
                Assert.IsType(typeof(NotFoundFileInfo), info);
            }
        }


        [Fact]
        public void GetFileInfo_ReturnsNotFoundFileInfo_IfNoDirectoryItems()
        {
            // Arrange
            var provider = new InMemoryFileProvider();

            // Act
            var fileInfo = provider.GetFileInfo("DoesNotExist.txt");

            // Assert
            Assert.NotNull(fileInfo);
            Assert.False(fileInfo.Exists);
        }



        [Fact]
        public void GetFileInfo_ReturnsNotFoundFileInfo_IfFileDoesNotExist()
        {
            // Arrange
            var provider = new InMemoryFileProvider();
            provider.Directory.AddFile("", new StringFileInfo("contents", "DoesExist.txt"));
            // Act
            var fileInfo = provider.GetFileInfo("DoesNotExist.txt");

            // Assert
            Assert.NotNull(fileInfo);
            Assert.False(fileInfo.Exists);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [InlineData("/C:\\Windows\\System32")]
        [InlineData("/\0/")]
        public void GetFileInfo_ReturnsNotFoundFileInfo_ForIllegalPathWithLeadingSlashes_Windows(string path)
        {
            GetFileInfo_ReturnsNotFoundFileInfo_ForIllegalPathWithLeadingSlashes(path);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Testing Unix specific behaviour on leading slashes.")]
        [InlineData("/\0/")]
        public void GetFileInfo_ReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes_Unix(string path)
        {
            GetFileInfo_ReturnsNotFoundFileInfo_ForIllegalPathWithLeadingSlashes(path);
        }

        public void GetFileInfo_ReturnsNotFoundFileInfo_ForIllegalPathWithLeadingSlashes(string path)
        {
            using (var provider = new InMemoryFileProvider())
            {
                var info = provider.GetFileInfo(path);
                Assert.IsType(typeof(NotFoundFileInfo), info);
            }
        }

        public static TheoryData<string> InvalidPaths
        {
            get
            {
                return new TheoryData<string>
                {
                    Path.Combine(". .", "file"),
                    Path.Combine(" ..", "file"),
                    Path.Combine(".. ", "file"),
                    Path.Combine(" .", "file"),
                    Path.Combine(". ", "file"),
                };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidPaths))]
        public void GetFileInfo_ReturnsNonExistentFileInfo_ForIllegalPath(string path)
        {
            using (var provider = new InMemoryFileProvider())
            {
                var info = provider.GetFileInfo(path);
                Assert.False(info.Exists);
            }
        }


        [Fact]
        public void GetFileInfo_ReturnsNotFoundFileInfo_ForRelativePathAboveRootPath()
        {
            using (var provider = new InMemoryFileProvider())
            {
                var info = provider.GetFileInfo(Path.Combine("..", Guid.NewGuid().ToString()));
                Assert.IsType(typeof(NotFoundFileInfo), info);
            }
        }


    }
}
