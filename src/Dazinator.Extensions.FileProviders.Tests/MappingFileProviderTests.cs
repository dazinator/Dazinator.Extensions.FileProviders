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


        // Format goes:
        // - Arg Index 0 = requestFolderPath. This ise path passed to the file provider when getting the directory contents. e.g "/foo/bar"
        // - Arg Index 1 = expectedResults. This is a string array containing the expected results. Each string in the array corresponds to a FileInfo.Name of na item that is expected to be returned in the IDirectoryContents result. The order does not need to match. 
        // - Arg Index 2 = fileMappings. This as an array, where each item corresponds to an explicit file mapping that will setup for the test. Each string entry, is split on the colon ":", to establish: [0]- the request path the file should be mapped on (i.e as a client will provide it) and [1] the source path the file is mapped to in the underlying source file provider. A dummy source file is automatically created in the InMemory file provider matching the source file path.
        // - Arg Index 3 = patternMappings. This is an array, where each item corresponds to pattern mapping. Each item is split on a colon ":" to give two parts: [0] and [1]. The first part [0] is split on a comma "," into [0]-A and [0]-B. [0]-A is the request path under which the pattern mapping will be added. e.g /foo/bar. [0]-B is the pattern itself e.g "**".
        //      The second part, [1], relates to a comma delimited string defining the files that will be created in the underlying source file provider backing the pattern mapping. These files will automatically be created in the InMemoryFile provider behind the patten mapping ready for resolution.
        [Theory]
        [InlineData("/foo", new string[] { "root.txt" }, new string[] { "foo/root.txt:bar/a.txt" })]
        [InlineData("/foo", new string[] { "root.txt" }, new string[] { "foo/root.txt:foo/bar.txt" })]
        [InlineData("/foo", new string[] { "root.txt" }, new string[] { "foo/root.txt:bar.txt" })]
        [InlineData("/", new string[] { "foo" }, new string[] {
            "foo/a.txt:foo/a.txt",
            "foo/b.txt:foo/b.txt"
        })] // get root, single subfolder expected
        [InlineData("/", new string[] { "foo", "bar" }, new string[] {
            "foo/a.txt:foo/a.txt",
            "bar/b.txt:bar/b.txt"
        })] // get root, 2 subfolders expected
        [InlineData("/", new string[] { "foo" }, new string[0],
            new string[] { "/,**:/foo" })] // get root, 1 subfolder via pattern match expected
        [InlineData("/foo/bar", new string[] { "a.txt" }, new string[0],
            new string[] { "/,**:/foo/bar/a.txt" })] // pattern matching file.
        [InlineData("/foo/bar", new string[] { "a.txt", "b.txt" }, new string[0],
            new string[] { "/,**:/foo/bar/a.txt,/foo/bar/b.txt" })] // pattern matching multiple files.
        [InlineData("/foo/bar", new string[] { "a.txt", "b.txt" }, new string[0],
            new string[] { "/,**/*.txt:/foo/bar/a.txt,/foo/bar/a.csv,/foo/bar/b.txt" })] // pattern matching multiple files but not some files.
        public async Task Can_Get_DirectoryContents(string requestFolderPath, string[] expectedResults, string[] fileMappings, string[] patternMappings = null)
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
                        folderPathMappings.AddFileNameMapping($"{filename}", inMemoryFileProvider, mapping.Item2);
                    }
                });
            }

            // setup pattern mappings
            var patternMappingsList = new List<Tuple<string, string, string>>();
            if (patternMappings != null)
            {
                foreach (var item in patternMappings)
                {
                    var mapping = item.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    var patternMapSegments = mapping[0].Split(",");
                    patternMappingsList.Add(new Tuple<string, string, string>(patternMapSegments[1], mapping[1], patternMapSegments[0]));
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
                    var mappingRequestPath = new PathString($"{file}");
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
                //var patternPathString = new PathString($"/{patternSegment}");

                // patternPathString.SplitToDirectoryPathAndFileName(PathStringExtensions.PathSeperationStrategy.FileNameIsAlwaysLastSegment, out var directory, out var pattern);

                fileMap.MapPath(mapping.Item3, (folderPathMappings) =>
                {
                    if (!string.IsNullOrEmpty(mapping.Item1))
                    {
                        folderPathMappings.AddPatternMapping(mapping.Item1, inMemoryFileProvider);
                    }
                });

            }

            var sut = new MappingFileProvider(fileMap);
            var folderContents = sut.GetDirectoryContents(requestFolderPath);
            Assert.NotNull(folderContents);
            if (expectedResults == null)
            {
                Assert.False(folderContents.Exists);
                return;
            }

            var contentItems = folderContents.ToArray();
            Assert.Equal(expectedResults.Length, contentItems.Length);

            var contentItemNames = contentItems.Select(c => c.Name).ToArray();

            for (int i = 0; i < expectedResults.Length; i++)
            {
                var expected = expectedResults[i];
                Assert.Contains<string>(expected, contentItemNames);
            }

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

