using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using DotNet.Globbing;
using Dazinator.Extensions.FileProviders.GlobPatternFilter;
using System.Linq;
using System.Runtime;
using Dazinator.Extensions.FileProviders.Utils;

namespace Dazinator.Extensions.FileProviders.Mapping
{

    public class FileMap
    {

        private Dictionary<PathString, FileMap> Children { get; set; }
        private Dictionary<PathString, SourceFileInfo> FileNameMappings { get; set; } = null;
        private List<PatternInfo> PatternMappings { get; set; } = null;

        public PathString Path { get; set; } = "/"; // root
        public FileMap Parent { get; set; } = null;

        /// <summary>
        /// Navigates the path to the specified child, creating the child for each segment of the path if it doesn't exist.
        /// </summary>
        /// <param name="requestPath"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public FileMap NavigateTo(PathString requestPath, bool createIfNotExists = true)
        {
            // navigate to the parent node for this file path.
            var mapNode = this;
            var requestPathSegments = requestPath.Value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in requestPathSegments)
            {
                if (createIfNotExists)
                {
                    mapNode = mapNode.GetOrAddChild($"/{item}");
                }
                else
                {
                    mapNode = mapNode.GetChild($"/{item}");
                }
            }

            return mapNode;
        }

        /// <summary>
        /// Creates a map at the specified path and calles a delegate for you to configure the mappings at that path.
        /// </summary>
        /// <param name="requestPath"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public FileMap MapPath(PathString requestPath, Action<FileMap> configureMappings)
        {
            // navigate to the parent node for this file path.
            var map = NavigateTo(requestPath, createIfNotExists: true);
            configureMappings(map);
            return this;
        }


        /// <summary>
        /// Tries to navigate each segment of the <see cref="PathString"/> finding the existing child map for that segment. The out parameters include the map for the last segment that was matched, plus any remaining path segments that there was no map for.
        /// </summary>
        /// <param name="requestPath"></param>
        /// <param name="map"></param>
        /// <param name="remaining"></param>
        /// <returns></returns>
        public bool TryNavigateTo(PathString requestPath, out FileMap map, out PathString remaining)
        {
            // navigate to the node for this file path.
            var segments = requestPath.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            int depth = 0;
            remaining = null;
            //PathString matchedPath = "/";
            map = this;
            for (var i = 0; i < segments.Length; i++)
            {
                if (map.TryGetChild($"/{segments[i]}", out var child))
                {
                    map = child;
                    depth = depth + 1;
                    continue;
                }

                remaining = new PathString($"/{string.Join("/", segments[depth..])}");
                return false;
            }

            return true;
        }

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
        public FileMap GetOrAddChild(PathString path)
        {
            if (Children == null)
            {
                Children = new Dictionary<PathString, FileMap>();
            }
            if (Children.TryGetValue(path, out var child))
            {
                return child;
            }
            return AddChild(path);
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
        public FileMap AddFileNameMapping(PathString fileName, IFileProvider sourceFileProvider, string sourcePath)
        {
            if (FileNameMappings == null)
            {
                FileNameMappings = new Dictionary<PathString, SourceFileInfo>();
            }

            FileNameMappings.Add(fileName, new SourceFileInfo()
            {
                SourceFileProvider = sourceFileProvider,
                SourcePath = sourcePath
            });

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
        private bool TryGetFileFromFileMappings(PathString fileName, out IFileInfo sourceFile)
        {
            sourceFile = null;
            if (FileNameMappings == null)
            {
                return false;
            }
            if (FileNameMappings.TryGetValue(fileName, out var sourceFileMapping))
            {
                sourceFile = GetMappedFileFromSourceFile(fileName, sourceFileMapping);
                return true;
            }

            return false;
        }

        private static IFileInfo GetMappedFileFromSourceFile(PathString fileName, SourceFileInfo sourceFileMapping)
        {
            IFileInfo sourceFile = sourceFileMapping.GetSourceFileInfo();

            // We need to override the name of the underlying source file to match the name specified in the mapping.
            // - but only if they are different.
            var requestFileName = fileName.Value.Substring(1);
            if (!string.Equals(sourceFile.Name, requestFileName, FileSystemStringComparison.SingletonInstance))
            {
                sourceFile = new WrappedFileInfo(sourceFile) { Name = requestFileName };
            }

            return sourceFile;
        }

        private bool TryGetFileFromPatternMappings(PathString path, out IFileInfo sourceFile)
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
            if (TryGetFileFromFileMappings(path, out sourceFile))
            {
                return true;
            }

            return TryGetFileFromPatternMappings(path, out sourceFile);
        }

        public IDirectoryContents GetMappedDirectoryContents()
        {
            var contents = new List<IFileInfo>();

            if (FileNameMappings != null)
            {
                foreach (var fileMapping in FileNameMappings)
                {
                    var mappedFileInfo = GetMappedFileFromSourceFile(fileMapping.Key, fileMapping.Value);
                    contents.Add(mappedFileInfo);
                }
            }

            // now map in explicitly named children - these represent folders.
            if (Children != null)
            {
                foreach (var child in Children)
                {

                    contents.Add(new DirectoryFileInfo(child.Key.Value.Substring(1)));
                    //var child = GetMappedFileFromSourceFile(fileMapping.Key, fileMapping.Value);
                    //  contents.Add(child);
                }

            }

            if (PatternMappings != null)
            {
                throw new NotImplementedException();
            }
            return new EnumerableDirectoryContents(contents.ToArray());
        }


        public class SourceFileInfo
        {
            public string SourcePath { get; set; }
            // public string TargetPath { get; set; }
            public IFileProvider SourceFileProvider { get; set; }

            public IFileInfo GetSourceFileInfo()
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
            public bool TryGetMatchedFileInfo(PathString filePath, out IFileInfo matchedFile)
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

        //private sealed class FileNameComparer : IEqualityComparer<IFileInfo>
        //{
        //    public bool Equals(IFileInfo? x, IFileInfo? y) => string.Equals(x?.Name, y?.Name, _fsComparison);

        //    public int GetHashCode(IFileInfo obj) => obj.Name.GetHashCode(_fsComparison);
        //}
    }



}


