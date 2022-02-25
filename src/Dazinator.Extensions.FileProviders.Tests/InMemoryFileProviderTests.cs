using Xunit;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Threading;
using Dazinator.Extensions.FileProviders.InMemory;
using Dazinator.Extensions.FileProviders.InMemory.Directory;

namespace Dazinator.Extensions.FileProviders.Tests
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


        //[ConditionalTheory]
        //[OSSkipCondition(OperatingSystems.Linux, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        //[OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        //[InlineData("/")]
        //[InlineData("///")]
        //[InlineData("/\\/")]
        //[InlineData("\\/\\/")]
        //public void GetFileInfo_ReturnsNotFoundFileInfo_ForValidPathsWithLeadingSlashes_Windows(string path)
        //{
        //    GetFileInfo_ReturnsNotFoundFileInfo_ForValidPathsWithLeadingSlashes(path);
        //}

        //[ConditionalTheory]
        //[OSSkipCondition(OperatingSystems.Windows, SkipReason = "Testing Unix specific behaviour on leading slashes.")]
        //[InlineData("/")]
        //[InlineData("///")]
        //public void GetFileInfo_ReturnsNotFoundFileInfo_ForValidPathsWithLeadingSlashes_Unix(string path)
        //{
        //    GetFileInfo_ReturnsNotFoundFileInfo_ForValidPathsWithLeadingSlashes(path);
        //}

        //public void GetFileInfo_ReturnsNotFoundFileInfo_ForValidPathsWithLeadingSlashes(string path)
        //{
        //    using (var provider = new InMemoryFileProvider())
        //    {
        //        var info = provider.GetFileInfo(path);
        //        Assert.IsType(typeof(NotFoundFileInfo), info);
        //    }
        //}


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

        //[ConditionalTheory]
        //[OSSkipCondition(OperatingSystems.Linux, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        //[OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        //[InlineData("/C:\\Windows\\System32")]
        //[InlineData("/\0/")]
        //public void GetFileInfo_ReturnsNotFoundFileInfo_ForIllegalPathWithLeadingSlashes_Windows(string path)
        //{
        //    GetFileInfo_ReturnsNotFoundFileInfo_ForIllegalPathWithLeadingSlashes(path);
        //}

        //[ConditionalTheory]
        //[OSSkipCondition(OperatingSystems.Windows, SkipReason = "Testing Unix specific behaviour on leading slashes.")]
        //[InlineData("/\0/")]
        //public void GetFileInfo_ReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes_Unix(string path)
        //{
        //    GetFileInfo_ReturnsNotFoundFileInfo_ForIllegalPathWithLeadingSlashes(path);
        //}

        //public void GetFileInfo_ReturnsNotFoundFileInfo_ForIllegalPathWithLeadingSlashes(string path)
        //{
        //    using (var provider = new InMemoryFileProvider())
        //    {
        //        var info = provider.GetFileInfo(path);
        //        Assert.IsType(typeof(NotFoundFileInfo), info);
        //    }
        //}

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

        [Fact]
        public void GetFileInfo_ReturnsNotFoundFileInfo_ForRelativePathThatNavigatesAboveRoot()
        {
            var rootDir = new InMemoryDirectory("root");

            // add a file at root/a.txt
            rootDir.AddFile("", new StringFileInfo("some content", "a.txt"));

            using (var provider = new InMemoryFileProvider(rootDir))
            {
                // Act
                var info = provider.GetFileInfo("../root/a.txt");
                Assert.IsType(typeof(NotFoundFileInfo), info);

            }
        }

        [Fact]
        public void GetFileInfo_ReturnsNotFoundFileInfo_ForRelativePathWithEmptySegmentsThatNavigates()
        {

            var rootDir = new InMemoryDirectory("root");

            // add a file at root/a.txt
            rootDir.AddFile("", new StringFileInfo("some content", "b"));

            using (var provider = new InMemoryFileProvider(rootDir))
            {
                var info = provider.GetFileInfo("a///../../" + "root" + "/b");
                Assert.IsType(typeof(NotFoundFileInfo), info);
            }

        }

        [Fact]
        public void CreateReadStream_Succeeds_OnEmptyFile()
        {

            var rootDir = new InMemoryDirectory("root");

            // add a file at root/a.txt
            rootDir.AddFile("", new StringFileInfo(null, "a.txt"));

            using (var provider = new InMemoryFileProvider(rootDir))
            {
                var info = provider.GetFileInfo("root/a.txt");
                using (var stream = info.CreateReadStream())
                {
                    Assert.NotNull(stream);
                }
            }

        }

        [Fact]
        public void Token_IsSame_ForSamePath()
        {

            var rootDir = new InMemoryDirectory("root");

            // add a file at root/a.txt
            rootDir.AddFile("", new StringFileInfo("some content", "b.txt"));

            using (var provider = new InMemoryFileProvider(rootDir))
            {
                var filePath = "root/b.txt";
                // var fileInfo = provider.GetFileInfo("root/b.txt");
                var token1 = provider.Watch(filePath);
                var token2 = provider.Watch(filePath);

                Assert.NotNull(token1);
                Assert.NotNull(token2);
                Assert.Equal(token2, token1);
            }

        }

        [Fact]
        public void TokensFiredOnFileChange()
        {

            var rootDir = new InMemoryDirectory("root");

            // add a file at root/a.txt
            var fileItem = rootDir.AddFile("", new StringFileInfo("some content", "b.txt"));

            using (var provider = new InMemoryFileProvider(rootDir))
            {
                var filePath = "root/b.txt";
                // var fileInfo = provider.GetFileInfo("root/b.txt");
                var token = provider.Watch(filePath);
                Assert.NotNull(token);
                Assert.False(token.HasChanged);
                Assert.True(token.ActiveChangeCallbacks);

                // Trigger a change.
                fileItem.Update(new StringFileInfo("modified content", "b.txt"));
                Assert.True(token.HasChanged);
            }

        }

        [Fact]
        public void TokenCallbackInvokedOnFileChange()
        {

            var rootDir = new InMemoryDirectory("root");

            // add a file at root/a.txt
            var fileItem = rootDir.AddFile("", new StringFileInfo("some content", "b.txt"));

            using (var provider = new InMemoryFileProvider(rootDir))
            {
                var filePath = "root/b.txt";
                // var fileInfo = provider.GetFileInfo("root/b.txt");
                var token = provider.Watch(filePath);
                Assert.NotNull(token);
                Assert.False(token.HasChanged);
                Assert.True(token.ActiveChangeCallbacks);

                bool callbackInvoked = false;
                token.RegisterChangeCallback(state =>
                {
                    callbackInvoked = true;
                }, state: null);

                // Trigger a change.
                fileItem.Update(new StringFileInfo("modified content", "b.txt"));
                Assert.True(callbackInvoked);
            }
        }

        [Fact]
        public void TokensFiredOnFileDeleted()
        {

            var rootDir = new InMemoryDirectory("root");

            // add a file at root/a.txt
            var fileItem = rootDir.AddFile("", new StringFileInfo("some content", "b.txt"));

            using (var provider = new InMemoryFileProvider(rootDir))
            {
                var filePath = "root/b.txt";
                // var fileInfo = provider.GetFileInfo("root/b.txt");
                var token = provider.Watch(filePath);
                Assert.NotNull(token);
                Assert.False(token.HasChanged);
                Assert.True(token.ActiveChangeCallbacks);

                bool callbackInvoked = false;
                token.RegisterChangeCallback(state =>
                {
                    callbackInvoked = true;
                }, state: null);

                // Trigger a change.
                fileItem.Delete();
                Assert.True(token.HasChanged);
                Assert.True(callbackInvoked);
            }

        }

        [Fact]
        public void WatchingWithEmptyStringArgumentShouldNotThrow()
        {
            // Arrange
            var rootDir = new InMemoryDirectory("root");
            using (var provider = new InMemoryFileProvider(rootDir))
            {
                // Act
                var ex = Record.Exception(() =>
                {
                    provider.Watch("");
                });
                // Assert
                Assert.Null(ex);
            }
        }

        [Fact]
        public void WatchingRootDirShouldTriggerAChangeOnAddingAFile()
        {
            // Arrange
            var rootDir = new InMemoryDirectory("root");
            using (var provider = new InMemoryFileProvider(rootDir))
            {
                var token = provider.Watch("");
                var callbackInvoked = false;
                token.RegisterChangeCallback(state =>
                {
                    callbackInvoked = true;
                }, state: null);

                // Act
                // add a file at root/a.txt
                var fileItem = rootDir.AddFile("", new StringFileInfo("some content", "b.txt"));

                // Assert
                Assert.True(token.HasChanged);
                Assert.True(callbackInvoked);
            }
        }

        [Fact]
        public void WatchingRootDirShouldNotTriggerAChangeOnAddingAFileToSubDir()
        {
            // Arrange
            var rootDir = new InMemoryDirectory("root");
            var subDir = rootDir.GetOrAddFolder("SubDir");
            using (var provider = new InMemoryFileProvider(rootDir))
            {
                var token = provider.Watch("");
                var callbackInvoked = false;
                token.RegisterChangeCallback(state =>
                {
                    callbackInvoked = true;
                }, state: null);

                // Act
                // add a file at root/a.txt
                var fileItem = subDir.AddFile(new StringFileInfo("some content", "b.txt"));

                // Assert
                Assert.False(token.HasChanged);
                Assert.False(callbackInvoked);
            }
        }

        [Fact]
        public void WatchingSubDirShouldTriggerAChangeOnAddingAFile()
        {
            // Arrange
            var rootDir = new InMemoryDirectory("root");
            var subDir = rootDir.GetOrAddFolder("SubDir");
            using (var provider = new InMemoryFileProvider(rootDir))
            {
                var token = provider.Watch("root/SubDir");
                var callbackInvoked = false;
                token.RegisterChangeCallback(state =>
                {
                    callbackInvoked = true;
                }, state: null);

                // Act
                // add a file at root/a.txt
                var fileItem = subDir.AddFile(new StringFileInfo("some content", "b.txt"));

                // Assert
                Assert.True(token.HasChanged);
                Assert.True(callbackInvoked);
            }
        }

        [Fact]
        public void WatchingSubDirShouldNotTriggerAChangeOnAddingAFileToRootDir()
        {
            // Arrange
            var rootDir = new InMemoryDirectory("root");
            var subDir = rootDir.GetOrAddFolder("SubDir");
            using (var provider = new InMemoryFileProvider(rootDir))
            {
                var token = provider.Watch("root/SubDir");
                var callbackInvoked = false;
                token.RegisterChangeCallback(state =>
                {
                    callbackInvoked = true;
                }, state: null);

                // Act
                // add a file at root/a.txt
                var fileItem = rootDir.AddFile("", new StringFileInfo("some content", "b.txt"));

                // Assert
                Assert.False(token.HasChanged);
                Assert.False(callbackInvoked);
            }
        }





        [Theory]
        [InlineData("", "appsettings.json")]
        [InlineData("/", "appsettings.json")]
        [InlineData("", "/appsettings.json", Skip = "Throws, not sure why yet")]
        [InlineData("/", "/appsettings.json", Skip = "Throws, not sure why yet")]
        public void Scenario_ConfigurationReloadTokensWork(string rootPath, string addjsonFilePath)
        {

            // Arrange
            var testFileProvider = new InMemoryFileProvider();
            testFileProvider.Directory.AddFile(rootPath, new StringFileInfo("{ \"Foo\": true }", "appsettings.json"));


            var services = new ServiceCollection();

            var configBuilder = new ConfigurationBuilder();
            //configBuilder.SetBasePath(Directory.GetCurrentDirectory());
            configBuilder.SetFileProvider(testFileProvider);

            configBuilder.AddJsonFile(addjsonFilePath, optional: false, reloadOnChange: true);

            IConfiguration config = configBuilder.Build();
            services.AddSingleton(config);

            var serviceProvider = services.BuildServiceProvider();
            //var childServiceProvider = services.CreateChildServiceProvider(serviceProvider, (childServices) =>
            //{
            //    // don't add any additional child registrations. so IConfiguration is purely in parent scope.
            //});


            var configInstance = serviceProvider.GetRequiredService<IConfiguration>();
            var reloadToken = configInstance.GetReloadToken();


            var waitHandle = new ManualResetEvent(false);
            reloadToken.RegisterChangeCallback((state) =>
            {
                waitHandle.Set();
            }, null);


            var configValue = configInstance["Foo"];
            Assert.Equal("True", configValue);


            // trigger reload token
            testFileProvider.Directory.AddOrUpdateFile(rootPath, new StringFileInfo("{ \"Foo\": false }", "appsettings.json"));
            Assert.True(waitHandle.WaitOne(TimeSpan.FromSeconds(5)));

            configValue = configInstance["Foo"];
            Assert.Equal("False", configValue);
        }


    }
}
