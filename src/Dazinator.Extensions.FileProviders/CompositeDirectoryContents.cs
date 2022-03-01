using Microsoft.Extensions.FileProviders;
using System.Collections;
using System.Collections.Generic;

namespace Dazinator.Extensions.FileProviders
{
    /// <summary>
    /// Represents a directory contents composed of other directory contents into a unified source.
    /// </summary>
    public class CompositeDirectoryContents : IDirectoryContents
    {
        private readonly IEnumerable<IDirectoryContents> _directoryContents;
        private bool _initialised = false;
        private bool _exists = false;
        private HashSet<string> _names = null;
        private List<IFileInfo> _files = null;


        public CompositeDirectoryContents(IEnumerable<IDirectoryContents> directoryContents)
        {
            _directoryContents = directoryContents;
        }
        public bool Exists
        {
            get
            {
                EnsureInitialised();
                return _exists;
            }
        }

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            EnsureInitialised();
            return _files.GetEnumerator();
        }

        private void EnsureInitialised()
        {
            if (_initialised == false)
            {
                _files = new List<IFileInfo>();
                _names = new HashSet<string>();

                foreach (var directory in _directoryContents)
                {
                    if (!_exists)
                    {
                        _exists = directory.Exists;
                    }

                    foreach (var file in directory)
                    {
                        if (_names.Add(file.Name))
                        {
                            _files.Add(file);
                        }
                    }
                }

                _initialised = true;

            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _files.GetEnumerator();
        }
    }

}
