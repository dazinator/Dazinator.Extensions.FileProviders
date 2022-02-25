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
            fileMap.WithRequestPath(fileDir, (mappings) =>
            {
                mappings.AddFileNameMapping(fileName, inMemoryFileProvider, sourcePath);
            });

            var sut = new MappingFileProvider(fileMap);
            var file = sut.GetFileInfo(requestFilePath);
            Assert.True(file.Exists);
        }

        [Theory]
        [InlineData("/foo", 1, "foo/root.txt:bar/a.txt")]
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
                fileMap.WithRequestPath(folderPath, (folderPathMappings) =>
                {
                    var fileName = mapping.Item1.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
                    folderPathMappings.AddFileNameMapping($"/{fileName}", inMemoryFileProvider, mapping.Item2);
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


            AddPatternMapping(pattern, inMemoryFileProvider, fileMap);
            //fileMap.WithRequestPath(fileDir, (folderPathMappings) =>
            //{
            //    folderPathMappings.AddPatternMapping(pattern, inMemoryFileProvider);
            //});

            //BuldFileMapWithPath(requestFilePath, out _, out _);
            //AddPatternMapping(pattern, inMemoryFileProvider, fileMap);

            var sut = new MappingFileProvider(fileMap);
            var file = sut.GetFileInfo(requestFilePath);
            Assert.True(file.Exists);
        }

        private static void AddPatternMapping(string pattern, InMemoryFileProvider inMemoryFileProvider, FileMap mapNode)
        {

            var filePathSegments = pattern.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var fileName = new PathString($"/{filePathSegments[^1]}");
            var fileDir = $"/{string.Join('/', filePathSegments[..^1])}";

            mapNode.WithRequestPath(fileDir, (folderPathMappings) =>
            {
                folderPathMappings.AddPatternMapping(fileName, inMemoryFileProvider);
            });
            //var patternSegments = pattern.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            //var patternDirectoryDirectorySegments = patternSegments.Length <= 1 ? Enumerable.Empty<string>() : patternSegments.Take(patternSegments.Length - 1);
            // mapNode.wih
            //  return mapNode;
        }

        //private static FileMap GetPathMapping(FileMap map, string requestFileName, out string fileName)
        //{
        //    var mapNode = map;
        //    var requestPathSegments = requestFileName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        //    var requestPathDirectorySegments = requestPathSegments.Length <= 1 ? Enumerable.Empty<string>() : requestPathSegments.Take(requestPathSegments.Length - 1);
        //    foreach (var item in requestPathDirectorySegments)
        //    {
        //        mapNode = mapNode.AddChild($"/{item}");
        //    }
        //    fileName = requestPathSegments.LastOrDefault();
        //    return mapNode;
        //}

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

