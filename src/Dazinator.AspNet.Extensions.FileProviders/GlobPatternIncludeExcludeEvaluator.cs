using DotNet.Globbing;
using System;
using System.Collections.Generic;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    public class GlobPatternIncludeExcludeEvaluator
    {
        private readonly string[] _includePatterns;
        private readonly string[] _excludePatterns;

        private readonly Lazy<List<Glob>> _includeGlobs;
        private readonly Lazy<List<Glob>> _excludeGlobs;

        public GlobPatternIncludeExcludeEvaluator(string[] includePatterns, string[] excludePatterns)
        {
            _includePatterns = includePatterns;
            _excludePatterns = excludePatterns;


            _includeGlobs = new Lazy<List<Glob>>(() =>
            {
                List<Glob> list = null;
                if (_includePatterns != null)
                {
                    list = new List<Glob>(_includePatterns.Length);
                    foreach (var pattern in _includePatterns)
                    {
                        var glob = Glob.Parse(pattern);
                        list.Add(glob);
                    }
                }
                return list;
            });

            _excludeGlobs = new Lazy<List<Glob>>(() =>
            {
                List<Glob> list = null;
                if (_excludePatterns != null)
                {
                    list = new List<Glob>(_excludePatterns.Length);
                    foreach (var pattern in _excludePatterns)
                    {
                        var glob = Glob.Parse(pattern);
                        list.Add(glob);
                    }
                }
                return list;
            });
        }


        public bool IsAllowed(string subject)
        {
            var includeGlobs = _includeGlobs.Value;
            if (includeGlobs == null)
            {
                return false;
            }

            bool isIncluded = false;
            foreach (var glob in includeGlobs)
            {
                isIncluded = glob.IsMatch(subject);
                if (isIncluded)
                {
                    break;
                }
            }

            if (!isIncluded)
            {
                return false;
            }

            var excludeGlobs = _excludeGlobs.Value;
            if (excludeGlobs == null)
            {
                return true;
            }

            foreach (var glob in excludeGlobs)
            {
                if (glob.IsMatch(subject))
                {
                    return false;
                }
            }

            return true;
        }


    }
}
