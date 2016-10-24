using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    public class SubPathInfo
    {
        private static char[] _directorySeperator = new char[] { '/' };

        // private string _directory;
        protected SubPathInfo(string directory, string name)
        {
            Directory = directory;
            Name = name;
            CheckPattern();
            CoerceNameToDirectoryIfNecessary();
            IsEmpty = string.IsNullOrWhiteSpace(Directory) && string.IsNullOrWhiteSpace(Name);
        }

        public string Name { get; private set; }

        public string Directory { get; private set; }

        public bool IsPattern { get; set; }

        public bool IsFile { get; set; }

        public bool IsEmpty { get; private set; }

        public void CheckPattern()
        {
            bool patternValidationEnabled = true;
            bool isWithinRangeBlock = false;
            var allChars = this.ToString();

            for (int i = 0; i <= allChars.Length - 1; i++)
            {
                var currentChar = allChars[i];
                if (patternValidationEnabled)
                {
                    if (IsStarOrQuestionMarkChar(currentChar))
                    {
                        IsPattern = true;
                    }
                    else if (IsStartRangeChar(currentChar))
                    {
                        if (isWithinRangeBlock)
                        {
                            throw new ArgumentException(
                                "Unsupported globbing pattern. A pattern cannot use nested [ characters. Expected a cosing bracket.");
                        }

                        isWithinRangeBlock = true;
                        IsPattern = true;
                    }
                    else if (IsEndRangeChar(currentChar))
                    {
                        // maybe at end of range block.
                        if (!isWithinRangeBlock)
                        {
                            throw new ArgumentException(
                                "Unsupported globbing pattern. A pattern cannot use a ] character without an opening [ character. Expected an opening bracket.");
                        }
                        isWithinRangeBlock = false;
                    }

                }
            }
        }

        public void CoerceNameToDirectoryIfNecessary()
        {

            // This is tricky and not perfect, because paths like the following could all be either
            // files or directories.
            // somefolder/.git
            // somefolder/folder.old
            // somefolder/somefile.old

            // Therefore we use a simplistic heuristic
            // For a path to be considered a file:
            //  1. It can't be pattern like (i.e it can;t contain * or ? etc.
            //  2. It can't end in a "/" (this is the same as the Name being null or empty)
            //  3. It must have a "." in the Name, but not at the end.
            // return !IsPattern && !string.IsNullOrWhiteSpace(Name) && Name.Contains('.') && !Name.EndsWith(".");

            bool isEmptyName = string.IsNullOrWhiteSpace(Name);
            IsFile = !IsPattern && !isEmptyName && Name.Contains('.') && !Name.EndsWith(".");
            // If we detected that the name should be treated as a directory, then appened it to the directory, and blank the name.
            if (!IsFile)
            {
                if (!isEmptyName)
                {
                    if (!string.IsNullOrWhiteSpace(Directory))
                    {
                        Directory = Directory + "/" + Name;
                    }
                    else
                    {
                        Directory = Name;
                    }

                    Name = string.Empty;
                }
            }



        }

        public static SubPathInfo Parse(string subpath)
        {
            if (string.IsNullOrWhiteSpace(subpath))
            {
                return new SubPathInfo(string.Empty, string.Empty);
                // throw new ArgumentException("subpath");
            }

            var builder = new StringBuilder(subpath.Length);

            var indexOfLastSeperator = subpath.LastIndexOf('/');
            if (indexOfLastSeperator != -1)
            {
                // has directory portion.
                for (int i = 0; i <= indexOfLastSeperator; i++)
                {
                    var currentChar = subpath[i];
                    if (currentChar == '/')
                    {
                        if (i == 0 || i == indexOfLastSeperator) // omit a starting and trailing slash (/)
                        {
                            continue;
                        }
                    }

                    builder.Append(currentChar);
                }
            }

            var directory = builder.ToString();
            builder.Clear();

            // now append Name portion
            if (subpath.Length > indexOfLastSeperator + 1)
            {
                for (int c = indexOfLastSeperator + 1; c < subpath.Length; c++)
                {
                    var currentChar = subpath[c];
                    builder.Append(currentChar);
                }
            }

            var name = builder.ToString();
            var subPath = new SubPathInfo(directory, name);
            return subPath;
        }

        public static bool IsStarOrQuestionMarkChar(char currentChar)
        {
            if (currentChar == '*' || currentChar == '?')
            {
                return true;
            }

            return false;
        }

        public static bool IsStartRangeChar(char currentChar)
        {
            if (currentChar == '[')
            {
                return true;
            }

            return false;
        }

        public static bool IsEndRangeChar(char currentChar)
        {
            if (currentChar == ']')
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Directory))
            {
                return Name;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                return Directory;
            }

            if (Directory.EndsWith("/"))
            {
                return $"{Directory}{Name}";
            }

            return $"{Directory}/{Name}";

        }

        public bool IsMatch(SubPathInfo subPath)
        {
            if (subPath.IsPattern)
            {
                return this.Like(subPath.ToString());
            }
            if (string.IsNullOrEmpty(subPath.Name) && IsInSameDirectory(subPath))
            {
                return true;
            }
            return this.Equals(subPath);
        }

        public bool IsInSameDirectory(SubPathInfo directory)
        {
            var match = directory.Directory == Directory;
            return match;
        }

        /// <summary>
        /// Compares the subpath against a given pattern.
        /// </summary>
        /// <param name="pattern">The pattern to match, where "*" means any sequence of characters, and "?" means any single character.</param>
        /// <returns><c>true</c> if the subpath matches the given pattern; otherwise <c>false</c>.</returns>
        public bool Like(string pattern)
        {

            var regex = new Regex("^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var path = this.ToString();

            var isMatch = regex.IsMatch(path);
            return isMatch;
        }

        public override bool Equals(object obj)
        {
            // If parameter cannot be cast to Point return false.
            SubPathInfo p = obj as SubPathInfo;
            if (p == null)
            {
                string pathString = obj as string;
                if (pathString == null)
                {
                    return false;
                }
                try
                {
                    p = SubPathInfo.Parse(pathString);
                }
                catch (Exception e)
                {
                    return false;
                }
            }

            // Return true if the fields match:
            //todo: make case sensitivity depend on platform?
            // ie windows file system is not case sensitive..

            return (Directory.Equals(p.Directory, StringComparison.OrdinalIgnoreCase))
                   && (Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
        }

        public override int GetHashCode()
        {
            int hashCode = Directory.GetHashCode() + Name.GetHashCode();
            return hashCode;
        }

        public string[] GetDirectorySegments()
        {
            return PathUtils.SplitPathIntoSegments(this.ToString());
        }

    }
}