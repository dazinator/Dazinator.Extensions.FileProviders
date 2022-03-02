using System;

namespace Microsoft.AspNetCore.Http
{
    public static class PathStringExtensions
    {
        public static void SplitToDirectoryPathAndFileName(this PathString path, PathSeperationStrategy strategy, out PathString directory, out PathString fileName)
        {
            var segments = path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var lastSegment = segments[^1];
            // assumption for test data purposes, if last segment of request path ends in "." we expect this
            // to be a file.
            // otherwise we expect it to represent a directory.
            if (strategy == PathSeperationStrategy.FileNameMustBeLastSegmentAndContainDotExtension)
            {
                bool isFilePath = lastSegment.Contains(".");
                if (isFilePath)
                {
                    // take all segments EXCEPT the file name to mean the directory portion of the path.
                    directory = $"/{string.Join('/', segments[..^1])}";
                    fileName = $"/{lastSegment}";
                }
                else
                {
                    // The entire path is a directory.
                    directory = path;
                    fileName = null;
                }
            }
            else if (strategy == PathSeperationStrategy.FileNameIsAlwaysLastSegment)
            {
                directory = $"/{string.Join('/', segments[..^1])}";
                fileName = $"/{lastSegment}";
            }
            else
            {
                throw new NotSupportedException();
            }

        }



        public enum PathSeperationStrategy
        {
            FileNameMustBeLastSegmentAndContainDotExtension = 0,
            FileNameIsAlwaysLastSegment = 1
        }
    }
}