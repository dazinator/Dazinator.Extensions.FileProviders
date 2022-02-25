using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using System;
using Dazinator.Extensions.FileProviders.Mapping.StaticWebAssets;
using System.IO;
using static Dazinator.Extensions.FileProviders.Mapping.StaticWebAssets.StaticWebAssetManifest;
using DotNet.Globbing;
using Dazinator.Extensions.FileProviders.GlobPatternFilter;

namespace Dazinator.Extensions.FileProviders.Mapping
{

    public class FileMap
    {
        private Dictionary<PathString, FileMap> Children { get; set; }
        private Dictionary<PathString, SourceFileInfo> FileMappings { get; set; } = null;
        private List<PatternInfo> PatternMappings { get; set; } = null;

        public PathString Path { get; set; } = "/"; // root
        public FileMap Parent { get; set; } = null;

        public bool TryGetChild(PathString path, out FileMap child)
        {
            child = null;
            if (Children == null)
            {
                return false;
            }
            return Children.TryGetValue(path, out child);

        }

        public FileMap GetChild(PathString path)
        {
            if (Children == null)
            {
                throw new InvalidOperationException("No child added with path specified.");
            }
            return Children[path];

        }
        public FileMap AddChild(PathString pathString)
        {
            if (Children == null)
            {
                Children = new Dictionary<PathString, FileMap>();
            }

            var child = new FileMap();
            Children.Add(pathString, child);
            child.Path = pathString;
            child.Parent = this;
            return child;
        }
        public FileMap AddFileMapping(PathString path, IFileProvider sourceFileProvider, string sourcePath)
        {
            if (FileMappings == null)
            {
                FileMappings = new Dictionary<PathString, SourceFileInfo>();
            }

            FileMappings.Add(path, new SourceFileInfo() { SourceFileProvider = sourceFileProvider, SourcePath = sourcePath });
            return this;
        }
        /// <summary>
        /// When this node or its descendents do not have an explicit file mapping to a source file,
        /// the nearest descendent node (the one that has matched most path segments (with no non matches),
        /// has it's PatternMappings evaluated. If the patterns match then file is requested from the file provider 
        /// present on the pattern mapping. If no patterns match, then the parents node is checked. We walk up the hierarchy back
        /// to the root node performing these pattern checks.
        /// To Summara
        /// </summary>
        public FileMap AddPatternMapping(string pattern, IFileProvider sourceFileProvider)
        {
            if (PatternMappings == null)
            {
                PatternMappings = new List<PatternInfo>();
            }

            PatternMappings.Add(new PatternInfo(Glob.Parse(pattern), sourceFileProvider));
            return this;
        }
        public bool TryGetFileMapping(PathString path, out SourceFileInfo sourceFile)
        {
            sourceFile = null;
            if (FileMappings == null)
            {
                return false;
            }
            return FileMappings.TryGetValue(path, out sourceFile);

        }
        public bool TryGetFileFromFileMappings(PathString path, out IFileInfo sourceFile)
        {
            sourceFile = null;
            if (FileMappings == null)
            {
                return false;
            }
            if (FileMappings.TryGetValue(path, out var sourceFileMapping))
            {
                sourceFile = sourceFileMapping.GetFileInfo();
                return true;
            }

            return false;
        }

        public bool TryGetFileFromPatternMappings(PathString path, out IFileInfo sourceFile)
        {
            sourceFile = null;
            if (PatternMappings == null)
            {
                return false;
            }

            foreach (var patternMapping in PatternMappings)
            {
                if (patternMapping.TryGetMatchedFileInfo(path, out sourceFile))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetMappedFile(PathString path, out IFileInfo sourceFile)
        {
            sourceFile = null;
            if (TryGetFileFromFileMappings(path, out sourceFile))
            {
                return true;
            }

            return TryGetFileFromPatternMappings(path, out sourceFile);
        }

        public class SourceFileInfo
        {
            public string SourcePath { get; set; }
            // public string TargetPath { get; set; }
            public IFileProvider SourceFileProvider { get; set; }

            public IFileInfo GetFileInfo()
            {
                return SourceFileProvider.GetFileInfo(SourcePath);
            }


        }

        public class PatternInfo
        {
            private readonly Glob _glob;
            private readonly IFileProvider _fileProvider;

            public PatternInfo(Glob glob, IFileProvider fileProvider)
            {
                _glob = glob;
                _fileProvider = fileProvider;
            }
            public bool TryGetMatchedFileInfo(string filePath, out IFileInfo matchedFile)
            {
                if (_glob.IsMatch(filePath))
                {
                    matchedFile = _fileProvider.GetFileInfo(filePath);
                    return true;
                }
                matchedFile = null;
                return false;
            }

        }
    }


    public static class FileMapExtensions
    {
        public static FileMap AddFromStaticWebAssetsManifest(this FileMap map, StaticWebAssetManifest manifest, Func<string, IFileProvider> contentRootFileProviderFactory)
        {
            PopulateFromManifest(map, manifest, contentRootFileProviderFactory);
            return map;
        }

        private static void PopulateFromManifest(FileMap map,
            StaticWebAssetManifest manifest,
            Func<string, IFileProvider> contentRootFileProviderFactory)
        {
            IFileProvider[] fps = new IFileProvider[manifest.ContentRoots.Length];
            for (int i = 0; i < fps.Length; i++)
            {
                fps[i] = contentRootFileProviderFactory(manifest.ContentRoots[i]);
                // patterns[i] = new Tuple<List<string>, List<string>>(new List<string>(), new List<string>());
            }

            Stack<Tuple<string, StaticWebAssetNode, FileMap>> stack = new Stack<Tuple<string, StaticWebAssetNode, FileMap>>();
            stack.Push(new Tuple<string, StaticWebAssetNode, FileMap>(string.Empty, manifest.Root, map));

            while (stack.TryPop(out var tuple))
            {
                var key = tuple.Item1;
                var node = tuple.Item2;
                var requestPathNode = tuple.Item3;

                if (node.Patterns != null)
                {
                    foreach (var pattern in node.Patterns)
                    {
                        var fp = fps[pattern.ContentRoot];
                        requestPathNode.AddPatternMapping(pattern.Pattern, fp);
                    };
                }

                // This should always be null, but could technically be set for root node.
                if (node.Match != null)
                {
                    if (requestPathNode.Parent != null) // are we processing root node
                    {
                        throw new InvalidOperationException("file mappings should be added to parent node");
                    }
                    // if it has an asset, we see this as an individual file mapping that should be added under
                    // the previuos request path node. i.e we don't want new request path nodes added per mapped file.
                    // we wan't a single one for the parent path, which has patterns, and files added to it as seperate concepts individually.
                    var asset = node.Match;
                    var fp = fps[asset.ContentRoot];
                    requestPathNode.AddFileMapping($"/{key}", fp, asset.Path);
                }

                if (node.Children == null)
                {
                    continue;
                }

                // stack children t
                foreach (var child in node.Children)
                {
                    // We are only interested in creating additional `RequestPathNode`s to represent each "parent"
                    // segment of the request path,
                    // so, foo/bar/bat.txt and foo/bar/baz.txt--> 
                    //   -foo
                    //     -bar
                    // then the "bat.txt" and "baz.txt" will be added as "file mappings" to the "bar" parent RequestPathNode.
                    // we detect if the child represents a "file mapping" by checking the "Match" property which
                    // points to a mapped file when that is the case.            

                    var asset = child.Value?.Match;
                    if (asset != null)
                    {
                        var fp = fps[asset.ContentRoot];
                        requestPathNode.AddFileMapping($"/{child.Key}", fp, asset.Path);
                    }
                    else
                    {
                        // must be processed as a potential parent.                       
                        var nextParentNode = requestPathNode.AddChild($"/{child.Key}");
                        stack.Push(new Tuple<string, StaticWebAssetNode, FileMap>(child.Key, child.Value, nextParentNode));
                    }

                }
            }
        }


        //private StaticWebAssetManifest LoadStaticWebAssetManifest(Stream manifestFileStream)
        //{
        //    var manifest = StaticWebAssetManifest.Parse(manifestFileStream);
        //    return manifest;
        //}

    }



}


