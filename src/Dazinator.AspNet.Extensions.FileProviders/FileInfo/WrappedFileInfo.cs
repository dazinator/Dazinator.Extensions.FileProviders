using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public class WrappedFileInfo : IFileInfo
    {

        private readonly IFileInfo _originalFileInfo;

        private bool? _Exists = null;
        private long? _Length = null;
        private string _PhysicalPath = null;
        private string _Name = null;
        private DateTimeOffset? _LastModified = null;
        private bool? _IsDirectory = null;

        public WrappedFileInfo(IFileInfo originalFileInfo)
        {
            _originalFileInfo = originalFileInfo;
        }

        public Stream CreateReadStream()
        {
            return _originalFileInfo.CreateReadStream();
        }

        public bool Exists
        {
            get
            {
                if (_Exists != null)
                {
                    return _Exists.Value;
                }
                else
                {
                    return _originalFileInfo.Exists;
                }
            }
            set
            {
                _Exists = value;
            }
        }
        public long Length
        {
            get
            {
                if (_Length != null)
                {
                    return _Length.Value;
                }
                else
                {
                    return _originalFileInfo.Length;
                }
            }
            set
            {
                _Length = value;
            }
        }
        public string PhysicalPath
        {
            get
            {
                if (_PhysicalPath != null)
                {
                    return _PhysicalPath;
                }
                else
                {
                    return _originalFileInfo.PhysicalPath;
                }
            }
            set
            {
                _PhysicalPath = value;
            }
        }
        public string Name
        {
            get
            {
                if (_Name != null)
                {
                    return _Name;
                }
                else
                {
                    return _originalFileInfo.Name;
                }
            }
            set
            {
                _Name = value;
            }
        }
        public DateTimeOffset LastModified
        {
            get
            {
                if (_LastModified != null)
                {
                    return _LastModified.Value;
                }
                else
                {
                    return _originalFileInfo.LastModified;
                }
            }
            set
            {
                _LastModified = value;
            }
        }
        public bool IsDirectory
        {
            get
            {
                if (_IsDirectory != null)
                {
                    return _IsDirectory.Value;
                }
                else
                {
                    return _originalFileInfo.IsDirectory;
                }
            }
            set
            {
                _IsDirectory = value;
            }
        }

    }
}