using System;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    public static class PathUtils
    {
        private static char[] _directorySeperator = new char[] { '/' };

        public static string[] SplitPathIntoSegments(string path)
        {
            string[] segments = path?.Split(_directorySeperator, StringSplitOptions.RemoveEmptyEntries);
            if (segments == null)
            {
                throw new ArgumentException("invalid directory. Directory must have atleast one segment.");
            }

            return segments;
        }
    }
}