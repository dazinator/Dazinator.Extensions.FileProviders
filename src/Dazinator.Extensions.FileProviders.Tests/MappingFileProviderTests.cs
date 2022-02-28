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
using System.IO;

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
        [InlineData("/foo", 1, new string[] { "foo/root.txt:bar/a.txt" })]
        [InlineData("/foo", 1, new string[] { "foo/root.txt:foo/bar.txt" })]
        [InlineData("/foo", 1, new string[] { "foo/root.txt:bar.txt" })]
        [InlineData("/", 1, new string[] {
            "foo/a.txt:foo/a.txt",
            "foo/b.txt:foo/b.txt"
        })] // get root, single subfolder expected
        [InlineData("/", 2, new string[] {
            "foo/a.txt:foo/a.txt",
            "bar/b.txt:bar/b.txt"
        })] // get root, 2 subfolders expected
        [InlineData("/", 1, new string[0],
            new string[] { "/**:/foo" })] // get root, 1 subfolder via pattern match expected
        public async Task Can_Get_DirectoryContents_Containing_ExplicitFiles(string folderPath, int expectedResults, string[] fileMappings, string[] patternMappings = null)
        {
            var inMemoryFileProvider = new InMemoryFileProvider();
            var mappings = new List<Tuple<string, string>>();

            foreach (var item in fileMappings)
            {
                var mapping = item.Split(':', StringSplitOptions.RemoveEmptyEntries);
                mappings.Add(new Tuple<string, string>(mapping[0], mapping[1]));
            }

            FileMap fileMap = new FileMap();
            // add an explicit mapping for each file
            foreach (var mapping in mappings)
            {
                AddInMemoryFile(inMemoryFileProvider, mapping.Item2);

                var mappingRequestPath = new PathString($"/{mapping.Item1}");
                mappingRequestPath.SplitToDirectoryPathAndFileName(PathStringExtensions.PathSeperationStrategy.FileNameMustBeLastSegmentAndContainDotExtension, out var directory, out var filename);

                fileMap.MapPath(directory, (folderPathMappings) =>
                {
                    if (!string.IsNullOrEmpty(filename))
                    {
                        folderPathMappings.AddFileNameMapping($"/{filename}", inMemoryFileProvider, mapping.Item2);
                    }
                });
            }

            // setup pattern mappings
            var patternMappingsList = new List<Tuple<string, string>>();
            if (patternMappings != null)
            {
                foreach (var item in patternMappings)
                {
                    var mapping = item.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    patternMappingsList.Add(new Tuple<string, string>(mapping[0], mapping[1]));
                }
            }

            // add an pattern mapping for each pattern mapping in the list,
            // create any file or directories to support the mapping for the purposes of the test
            // as specified.
            foreach (var mapping in patternMappingsList)
            {
                // set up the test files in the underlying provider, used in this test of the mapping.
                var files = mapping.Item2.Split(",");
                foreach (var file in files)
                {
                    var mappingRequestPath = new PathString($"/{file}");
                    mappingRequestPath.SplitToDirectoryPathAndFileName(PathStringExtensions.PathSeperationStrategy.FileNameMustBeLastSegmentAndContainDotExtension, out var fileDirectory, out var filename);

                    if (!string.IsNullOrEmpty(filename))
                    {
                        AddInMemoryFile(inMemoryFileProvider, file);
                    }
                    else
                    {
                        inMemoryFileProvider.Directory.GetOrAddFolder(fileDirectory);
                    }
                }

                // add the mapping
                var patternSegment = mapping.Item1;
                // an assumption is made here about the format of the pattern provided in the test.
                // we will assume that the last segment is always the pattern and any segments before are directory
                // path that the pattern needs to mapped on to.
                var patternPathString = new PathString($"/{patternSegment}");
                patternPathString.SplitToDirectoryPathAndFileName(PathStringExtensions.PathSeperationStrategy.FileNameIsAlwaysLastSegment, out var directory, out var pattern);

                fileMap.MapPath(directory, (folderPathMappings) =>
                {
                    if (!string.IsNullOrEmpty(pattern))
                    {
                        folderPathMappings.AddPatternMapping(pattern.Value.Substring(1), inMemoryFileProvider);
                    }
                });

            }

            var sut = new MappingFileProvider(fileMap);
            var folderContents = sut.GetDirectoryContents(folderPath);
            Assert.NotNull(folderContents);
            Assert.Equal(expectedResults, folderContents.Count());
        }


        [Theory]
        [InlineData("root.txt", "root.txt", "**", null)]
        [InlineData("_content/foo/bar/a.txt", "a.txt", "_content/foo/bar/**", null)]
        [InlineData("_content/foo/txt/b.txt", "txt/b.txt", "_content/foo/**", null)]
        [InlineData("foo/bar/txt/b.txt", "foo/bar/txt/b.txt", "foo/bar/**", 0)]
        [InlineData("foo/bar/txt/b.txt", "bar/txt/b.txt", "foo/bar/**", 1)]
        [InlineData("foo/bar/txt/b.txt", "txt/b.txt", "foo/bar/**", 2)]
        public async Task Can_Get_File_From_Pattern_Mapping(
            string requestFilePath,
            string sourceFilePath,
            string pattern,
            int? patternSourceProviderPathDepth)
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
                folderPathMappings.AddPatternMapping(fileNamePattern, inMemoryFileProvider, patternSourceProviderPathDepth);
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

