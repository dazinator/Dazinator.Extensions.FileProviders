using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Dazinator.Extensions.FileProviders.Mapping;
using Dazinator.Extensions.FileProviders.InMemory;
using System.Linq;

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

            InMemoryFileProvider inMemoryFileProvider = BuildInMemoryFiles(sourcePath);
            FileMap fileMap = BuldFileMapWithPath(requestFilePath, out var requestPathNode, out string fileName);

            requestPathNode.AddFileMapping($"/{fileName}", inMemoryFileProvider, sourcePath);

            // var manifest = TestExtensions.LoadStaticWebAssetManifestFromEmbeddedResource(StaticWebAssetsFileResourcePath);
            var sut = new MappingFileProvider(fileMap);
            var file = sut.GetFileInfo(requestFilePath);
            Assert.True(file.Exists);
        }


        [Theory]
        [InlineData("root.txt", "root.txt", "**")]
        [InlineData("_content/foo/bar/a.txt", "a.txt", "_content/foo/bar/**")]
        [InlineData("_content/foo/txt/b.txt", "txt/b.txt", "_content/foo/**")]
        public async Task Can_Get_File_From_Pattern_Mapping(string requestFilePath, string sourceFilePath, string pattern)
        {
            InMemoryFileProvider inMemoryFileProvider = BuildInMemoryFiles(sourceFilePath);
            FileMap fileMap = BuldFileMapWithPath(requestFilePath, out _, out _);
            AddPatternMapping(pattern, inMemoryFileProvider, fileMap);

            var sut = new MappingFileProvider(fileMap);
            var file = sut.GetFileInfo(requestFilePath);
            Assert.True(file.Exists);
        }

        private static void AddPatternMapping(string pattern, InMemoryFileProvider inMemoryFileProvider, FileMap mapNode)
        {
            var patternSegments = pattern.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var patternDirectoryDirectorySegments = patternSegments.Length <= 1 ? Enumerable.Empty<string>() : patternSegments.Take(patternSegments.Length - 1);
            foreach (var item in patternDirectoryDirectorySegments)
            {
                mapNode = mapNode.GetChild($"/{item}");
            }
            mapNode.AddPatternMapping(patternSegments.LastOrDefault(), inMemoryFileProvider);
            //  return mapNode;
        }

        private static FileMap BuldFileMapWithPath(string requestFileName, out FileMap mapNode, out string fileName)
        {
            var fileMap = new FileMap();
            mapNode = fileMap;
            var requestPathSegments = requestFileName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var requestPathDirectorySegments = requestPathSegments.Length <= 1 ? Enumerable.Empty<string>() : requestPathSegments.Take(requestPathSegments.Length - 1);
            foreach (var item in requestPathDirectorySegments)
            {
                mapNode = mapNode.AddChild($"/{item}");
            }
            fileName = requestPathSegments.LastOrDefault();
            return fileMap;
        }

        private static InMemoryFileProvider BuildInMemoryFiles(string sourceFilePath)
        {
            var inMemoryFileProvider = new InMemoryFileProvider();

            var segments = sourceFilePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var sourceFileName = segments.LastOrDefault();
            var sourceDirectory = segments.Length <= 1 ? "/" : string.Join('/', segments.Take(segments.Length - 1));

            inMemoryFileProvider.Directory.AddFile(sourceDirectory, new StringFileInfo("test", sourceFileName));
            return inMemoryFileProvider;
        }
    }

}

