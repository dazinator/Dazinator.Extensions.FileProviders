using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Linq;

namespace Dazinator.Extensions.FileProviders.GlobPatternFilter
{
    public class GlobPatternFilterFileProvider : IFileProvider
    {
        private readonly IFileProvider _inner;
        private readonly GlobPatternIncludeExcludeEvaluator _evaluator;

        public GlobPatternFilterFileProvider(IFileProvider inner, string[] includes, string[] excludes = null)
        {
            _inner = inner;
            _evaluator = new GlobPatternIncludeExcludeEvaluator(includes, excludes);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            // check for and remove trailing / because folder names do not end with /.
            // The root folder is allowed to be called /.           
            if(subpath.Length > 1 && subpath?.Last() =='/')
            {
                subpath = subpath.Substring(0, subpath.Length -1);
            }
            var filteredResults = new GlobMatchingEnumerableFileInfos(subpath, false, _inner, _evaluator);
            return new GlobMatchingEnumerableDirectoryContents(filteredResults);           

        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if(!_evaluator.IsAllowed(subpath))
            {
                return new NotFoundFileInfo(subpath);
            }

            return _inner.GetFileInfo(subpath);           
        }

        public IChangeToken Watch(string filter)
        {
            // This isn't strictly implemented correctly yet as we just pass through watch to undelrying provider, but ideally we need to 
            // wrap the change token, and only signal when an allowed file changes (based on glob include / exclude patterns)
            // at the moment, this filtering isn't applied resulting in the change token firing even for file changes that
            // that technically should be filtered out.
           // var currentFiles = GetDirectoryContents(filter);

           var innerChangeToken = _inner.Watch(filter);

            //TODO: Work out how to only fire the change token when a file matching the glob filter has changed.
            //   notes: maybe take a snapshot of directory structure right now, and after the change, then if any new / deleted files, or files with last update times changed modified whose
            //          paths fall within the glob filter, only then signal the changetoken returned from this method?
            return innerChangeToken;         
        }



    }
}
