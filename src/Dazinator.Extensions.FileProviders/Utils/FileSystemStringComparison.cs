using System;

namespace Dazinator.Extensions.FileProviders.Utils
{
    public static class FileSystemStringComparison
    {
        private static readonly StringComparison _fsComparison = OperatingSystem.IsWindows() ?
StringComparison.OrdinalIgnoreCase :
StringComparison.Ordinal;

        public static StringComparison SingletonInstance => _fsComparison;
    }



}


