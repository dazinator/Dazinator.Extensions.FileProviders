using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Dazinator.Extensions.FileProviders.Mapping;
using Dazinator.Extensions.FileProviders.InMemory;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Dazinator.Extensions.FileProviders.Mapping.StaticWebAssets;

namespace Dazinator.Extensions.FileProviders.Tests
{
    public partial class MappingFileProviderTests
    {
        public class StaticWebAssetManifestTests
        {
            public const string StaticWebAssetsFileResourcePath = "Resources/TestDir.staticwebassets.runtime.json";

            [Fact]
            public async Task Can_Load_Manifest()
            {
                var manifest = TestExtensions.LoadStaticWebAssetManifestFromEmbeddedResource(StaticWebAssetsFileResourcePath);
                Assert.NotNull(manifest);
            }

        }

        public class FileMapTests
        {
            public const string StaticWebAssetsFileResourcePath = "Resources/TestDir.staticwebassets.runtime.json";

            [Fact]
            public async Task Can_Populate_From_StaticWebAssetsManifest()
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fp = new ManifestEmbeddedFileProvider(assembly);
                var manifestFile = fp.GetFileInfo(StaticWebAssetsFileResourcePath);
                var manifest = TestExtensions.LoadStaticWebAssetManifestFromEmbeddedResource(StaticWebAssetsFileResourcePath);

                var map = new FileMap();

                var inMemoryProviders = new List<InMemoryFileProvider>();
                map.AddFromStaticWebAssetsManifest(manifest, (contentRoot) =>
                {
                    var provider = new InMemoryFileProvider();
                    inMemoryProviders.Add(provider);
                    return provider;
                });

                Assert.True(map.TryGetChild("/_content", out var fileMap));
                Assert.NotNull(fileMap);

            }
        }

        [Theory]
        [InlineData("root.txt", "a.txt")]
        [InlineData("_content/foo/bar/a.txt", "txt/a.txt")]
        [InlineData("_content/foo/bar/renamed-b.txt", "txt/b.txt")]
        public async Task Can_Get_File_From_Explicit_File_Mapping(string requestFilePath, string sourcePath)
        {
            var inMemoryFileProvider = new InMemoryFileProvider();

            AddInMemoryFile(inMemoryFileProvider, sourcePath);
            var fileMap = new FileMap();

            var filePathSegments = requestFilePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var fileName = new PathString($"/{filePathSegments[^1]}");

            var fileDir = $"/{string.Join('/', filePathSegments[..^1])}";
            fileMap.MapPath(fileDir, (mappings) =>
            {
                mappings.AddFileNameMapping(fileName, inMemoryFileProvider, sourcePath);
            });

            var sut = new MappingFileProvider(fileMap);
            var file = sut.GetFileInfo(requestFilePath);

            Assert.True(file.Exists);
            Assert.Equal(fileName.Value.TrimStart('/'), file.Name);
        }

        [Theory]
        [InlineData("/foo", 1, "foo/root.txt:bar/a.txt")]
        [InlineData("/foo", 1, "foo/root.txt:foo/bar.txt")]
        [InlineData("/foo", 1, "foo/root.txt:bar.txt")]
        [InlineData("/", 1, "foo/a.txt:foo/a.txt", "foo/b.txt:foo/b.txt")] // single subfolder
        [InlineData("/", 2, "foo/a.txt:foo/a.txt", "bar/b.txt:bar/b.txt")] // multiple subfolders
        public async Task Can_Get_DirectoryContents_Containing_ExplicitFiles(string folderPath, int expectedResults, params string[] files)
        {
            var inMemoryFileProvider = new InMemoryFileProvider();
            var mappings = new List<Tuple<string, string>>();

            foreach (var item in files)
            {
                var mapping = item.Split(':', StringSplitOptions.RemoveEmptyEntries);
                mappings.Add(new Tuple<string, string>(mapping[0], mapping[1]));
            }

            FileMap fileMap = new FileMap();
            // add an explicit mapping for each file
            foreach (var mapping in mappings)
            {
                AddInMemoryFile(inMemoryFileProvider, mapping.Item2);
                var mappingRequestPath = $"/{mapping.Item1}";
                var mappingRequestPathSegments = mapping.Item1.Split('/', StringSplitOptions.RemoveEmptyEntries);

                var mappingRequestPathFileName = mappingRequestPathSegments[^1];
                // assumption for test data purposes, if last segment of request path ends in "." we expect this
                // to be a file.
                // otherwise we expect it to represent a directory.
                bool isFilePath = mappingRequestPathFileName.Contains(".");
                string mappingRequestPathDir;
                if (isFilePath)
                {
                    // take all segments EXCEPT the file name to mean the directory portion of the path.
                    mappingRequestPathDir = $"/{string.Join('/', mappingRequestPathSegments[..^1])}";
                }
                else
                {
                    // The entire path is a directory.
                    mappingRequestPathDir = mappingRequestPath;
                }


                fileMap.MapPath(mappingRequestPathDir, (folderPathMappings) =>
                {
                    if (isFilePath)
                    {
                        folderPathMappings.AddFileNameMapping($"/{mappingRequestPathFileName}", inMemoryFileProvider, mapping.Item2);
                    }
                });
            }

            var sut = new MappingFileProvider(fileMap);
            var folderContents = sut.GetDirectoryContents(folderPath);
            Assert.NotNull(folderContents);
            Assert.Equal(expectedResults, folderContents.Count());
        }


        [Theory]
        [InlineData("root.txt", "root.txt", "**")]
        [InlineData("_content/foo/bar/a.txt", "a.txt", "_content/foo/bar/**")]
        [InlineData("_content/foo/txt/b.txt", "txt/b.txt", "_content/foo/**")]
        public async Task Can_Get_File_From_Pattern_Mapping(
            string requestFilePath,
            string sourceFilePath,
            string pattern)
        {
            InMemoryFileProvider inMemoryFileProvider = new InMemoryFileProvider();
            AddInMemoryFile(inMemoryFileProvider, sourceFilePath);

            FileMap fileMap = new FileMap();
            // the pattern in the test data deliberatley takes the form [base-path]/[pattern]
            // where [base-path] and [pattern] are split here
            // to work out at which path to add the pattern mapping.
            var pathSegments = pattern.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var fileDir = $"/{string.Join('/', pathSegments[..^1])}";

            fileMap.MapPath(fileDir, (folderPathMappings) =>
            {
                var fileNamePattern = new PathString($"/{pathSegments[^1]}");
                folderPathMappings.AddPatternMapping(fileNamePattern, inMemoryFileProvider);
            });

            var sut = new MappingFileProvider(fileMap);
            var file = sut.GetFileInfo(requestFilePath);
            Assert.True(file.Exists);
        }

        private static InMemoryFileProvider AddInMemoryFile(InMemoryFileProvider inMemoryFileProvider, string sourceFilePath)
        {

            var segments = sourceFilePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var sourceFileName = segments.LastOrDefault();
            var sourceDirectory = segments.Length <= 1 ? "/" : string.Join('/', segments.Take(segments.Length - 1));

            inMemoryFileProvider.Directory.AddFile(sourceDirectory, new StringFileInfo("test", sourceFileName));
            return inMemoryFileProvider;
        }
    }

}

