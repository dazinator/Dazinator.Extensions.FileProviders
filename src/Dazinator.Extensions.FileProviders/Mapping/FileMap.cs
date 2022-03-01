using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using System;
using Dazinator.Extensions.FileProviders.GlobPatternFilter;
using Dazinator.Extensions.FileProviders.Utils;

namespace Dazinator.Extensions.FileProviders.Mapping
{

    public class FileMap
    {
        private readonly DirectoryFileInfo _cachedDirectoryInfo;
        private Lazy<IDirectoryContents> _explicitDirectoryContents;


        public FileMap() : this("/")
        {

        }

        public FileMap(PathString path)
        {
            Path = path;
            _cachedDirectoryInfo = string.IsNullOrEmpty(path) ? new DirectoryFileInfo(string.Empty) :
                new DirectoryFileInfo(path.Value.Substring(1)); // remove leading "/"
            _explicitDirectoryContents = new Lazy<IDirectoryContents>(GetExplicitDirectoryContents);
        }

        private IDirectoryContents GetExplicitDirectoryContents()
        {
            List<IFileInfo> contents = null;
            var explicitItemCount = (FileNameMappings?.Count ?? 0) + (Children?.Count ?? 0);

            if (explicitItemCount > 0)
            {
                contents = new List<IFileInfo>(explicitItemCount);

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
                        contents.Add(child.Value._cachedDirectoryInfo);
                    }
                }

            }

            if (contents == null)
            {
                return NotFoundDirectoryContents.Singleton;
            }

            return new EnumerableDirectoryContents(true, contents.ToArray());


        }


        private Dictionary<PathString, FileMap> Children { get; set; }
        private Dictionary<PathString, SourceFileInfo> FileNameMappings { get; set; } = null;
        private List<PatternInfo> PatternMappings { get; set; } = null;

        public PathString Path { get; private set; }
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
        public bool TryNavigateTo(PathString requestPath, out FileMap map, out PathString remaining, out string[] requestPathSegments)
        {
            // navigate to the node for this file path.
            requestPathSegments = requestPath.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            int depth = 0;
            remaining = null;
            //PathString matchedPath = "/";
            map = this;
            for (var i = 0; i < requestPathSegments.Length; i++)
            {
                if (map.TryGetChild($"/{requestPathSegments[i]}", out var child))
                {
                    map = child;
                    depth = depth + 1;
                    continue;
                }

                remaining = new PathString($"/{string.Join("/", requestPathSegments[depth..])}");
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

            var child = new FileMap(pathString);
            Children.Add(pathString, child);
            //child.Path = pathString;
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
        /// Adds a pattern mapping from this segment Path. 
        /// </summary>
        /// <param name="pattern">The glob pattern used to indicate matching files that will be included from the source file provider.</param>
        /// <param name="sourceFileProvider">The <see cref="IFileProvider"/> that will provide the source items backing this pattern.</param>
        /// <param name="segmentDepthOfPathToPassToSourceFileProvider">An index indicating the depth of the path (in terms of segments) to start from when building the source file path for requesting items from the source file provider for this pattern. 
        /// For example if depth is 2 and the full request path is /foo/bar/baz.txt then "baz.txt" is requested from the source provider. If the depth is 1, then "/bar/baz.txt" will be requested from the source file provider. Depth of 0 means "/foo/bar/baz.txt" i.e full path information will be passed to the source file provider.
        /// If you specify null for this, then the depth will default to the current depth of the segment you are adding this pattern mapping to. For example of you are adding a pattern mapping to "foo/bar" segment with pattern "**" then the depth will be equivalent to "2" meaning "/foo/bar" will not be passed to the source provider and only the remaining unmapped segments will be passed.</param>
        /// <returns></returns>
        public FileMap AddPatternMapping(string pattern, IFileProvider sourceFileProvider, int? segmentDepthOfPathToPassToSourceFileProvider = null)
        {
            if (PatternMappings == null)
            {
                PatternMappings = new List<PatternInfo>();
            }

            PatternMappings.Add(new PatternInfo(pattern, sourceFileProvider, segmentDepthOfPathToPassToSourceFileProvider));
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

        private bool TryGetFileFromPatternMappings(PathString fileName, string[] requestPathSegments, out IFileInfo sourceFile)
        {
            sourceFile = null;
            if (PatternMappings == null)
            {
                return false;
            }

            // var segments = fullRequestFilepath.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);

            foreach (var patternMapping in PatternMappings)
            {
                if (patternMapping.TryGetMatchedFileInfo(fileName, requestPathSegments, out sourceFile))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the file as mapped at this segment of the path.
        /// </summary>
        /// <param name="path">The remaining segments of the request path at this level of the mapping heirarchy.</param>
        /// <param name="fullRequestpath">The full request path.</param>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        public bool TryGetMappedFile(PathString remainingPath, string[] requestPathSegments, out IFileInfo sourceFile)
        {
            if (TryGetFileFromFileMappings(remainingPath, out sourceFile))
            {
                return true;
            }

            return TryGetFileFromPatternMappings(remainingPath, requestPathSegments, out sourceFile);
        }


        /// <summary>
        /// Gets the mapped directory contents.
        /// </summary>
        /// <param name="remainingPath">The remaining segments of the request path.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool GetMappedDirectoryContents(PathString remainingPath, string[] requestPathSegments, out IDirectoryContents directoryContents)
        {
            // Only include explcit contents when there is no remaining path - i.e these contents that are mapped
            // relate to the request path exactly and so we include them.    
            directoryContents = (remainingPath == null || remainingPath == "/") ? _explicitDirectoryContents.Value : NotFoundDirectoryContents.Singleton;

            // Patterns apply at all levels.
            IDirectoryContents patternMappedContents = null;
            if (PatternMappings != null)
            {
                var directoryContentsList = new List<IDirectoryContents>(PatternMappings.Count);

                foreach (var patternMapping in PatternMappings)
                {
                    if (patternMapping.TryGetMatchedDirectoryContents(remainingPath, requestPathSegments, out var matchedDirectoryContents))
                    {
                        directoryContentsList.Add(matchedDirectoryContents);
                    }
                }

                if (directoryContentsList.Count > 0)
                {
                    patternMappedContents = new CompositeDirectoryContents(directoryContentsList);
                }
            }

            if (patternMappedContents?.Exists ?? false)
            {
                if (directoryContents.Exists)
                {
                    directoryContents = new CompositeDirectoryContents(new IDirectoryContents[2]
                    { directoryContents, patternMappedContents });
                }
                else
                {
                    directoryContents = patternMappedContents;
                }
            }

            return directoryContents.Exists;

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
            // private readonly Glob _glob;
            private readonly IFileProvider _fileProvider;
            private readonly int? _sourcePathDepth = null;

            /// <summary>
            /// Create a new pattern mapping.
            /// </summary>
            /// <param name="glob"></param>
            /// <param name="fileProvider"></param>
            /// <param name="sourcePathDepth">An index indicating the depth of the path (in terms of segments) to start from when building the source file path for requesting items from the source file provider for this pattern. 
            /// For example if depth is 2 and the full request path is /foo/bar/baz.txt then "baz.txt" is requested from the source provider. If the depth is 1, then "/bar/baz.txt" will be requested from the source file provider. Depth of 0 means "/foo/bar/baz.txt" i.e full path information will be passed to the source file provider.
            /// If you specify null for this, then the depth will default to the current depth of the segment you are adding this pattern mapping to. For example of you are adding a pattern mapping to "foo/bar" segment with pattern "**" then the depth will be equivalent to "2" meaning "/foo/bar" will not be passed to the source provider and only the remaining unmapped segments will be passed.</param>
            public PatternInfo(string includePattern, IFileProvider fileProvider, int? sourcePathDepth = null)
            {
                //_glob = glob;
                //_fileProvider = fileProvider;
                _sourcePathDepth = sourcePathDepth; // when a request path comes in like

                _fileProvider = new GlobPatternFilterFileProvider(fileProvider, new string[1] { includePattern });
                // /foo/bar/bat.txt
                // 
            }

            /// <summary>
            /// Get the matching file from the source provider based on the specified file path.
            /// Only files that match the pattern will be considered.
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="fullRequestpathSegments"></param>
            /// <param name="matchedFile"></param>
            /// <returns></returns>
            public bool TryGetMatchedFileInfo(PathString filePath, string[] fullRequestpathSegments, out IFileInfo matchedFile)
            {
                PathString filePathToMatch;

                // If no depth explicitly configured on the pattern mapping,
                // then match the file path from the current segment depth where this
                // pattern mapping is added.
                if (_sourcePathDepth == null)
                {
                    filePathToMatch = filePath; // use 
                }
                else
                {
                    var candidateDirectoryPath = string.Join('/', fullRequestpathSegments[_sourcePathDepth.Value..]);
                    filePathToMatch = string.IsNullOrEmpty(candidateDirectoryPath) ? filePath : new PathString($"/{candidateDirectoryPath}");
                }

                //if (_glob.IsMatch(filePathToMatch))
                //{
                matchedFile = _fileProvider.GetFileInfo(filePathToMatch);
                if (matchedFile.Exists)
                {
                    return true;
                }
                // }
                matchedFile = null;
                return false;
            }

            /// <summary>
            /// Get the matching file and directories from the source provider based on any directory path within the provider (remaining path)
            /// Only items that match the pattern will be considered.
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="fullRequestpathSegments"></param>
            /// <param name="matchedFile"></param>
            /// <returns></returns>
            public bool TryGetMatchedDirectoryContents(PathString remainingPath, string[] fullRequestpathSegments, out IDirectoryContents matchedDirectoryContents)
            {
                PathString directoryPath;

                // If no depth explicitly configured on the pattern mapping,
                // then match the file path from the current segment depth where this
                // pattern mapping is added.
                // e.g with no depth set, the path passed to the source file provider is the relative path from the current mapped segment.
                // so if this pattern mapping was mapped on path: `/foo/bar` and the depth was 2 (or null) and the client requested `/foo/bar/bat.txt` then
                // then only the segments starting at index 2 (which is /bat.txt) would be passed to the source file provider.
                // With a depth of 1, "/bar/bat.txt" would be passed. With a depth of 0, the full path would be passed.
                // todo: this behaviour could be factored into an independent FileProvider and used in composition here..
                if (_sourcePathDepth == null)
                {
                    directoryPath = remainingPath; // use 
                }
                else
                {
                    var baseDirectoryPath = string.Join('/', fullRequestpathSegments[_sourcePathDepth.Value..]);
                    directoryPath = string.IsNullOrEmpty(baseDirectoryPath) ? remainingPath : new PathString($"/{baseDirectoryPath}");
                }

                var dirContents = _fileProvider.GetDirectoryContents(directoryPath);
                if (dirContents.Exists)
                {
                    matchedDirectoryContents = dirContents;
                    return true;
                }
                matchedDirectoryContents = null;
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


