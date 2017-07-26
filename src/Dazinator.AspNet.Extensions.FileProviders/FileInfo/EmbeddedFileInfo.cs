using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dazinator.AspNet.Extensions.FileProviders.FileInfo
{
    public class EmbeddedFileInfo : IFileInfo
    {

        private readonly Assembly _assembly;
        private readonly string _resourcePath;
        private Lazy<long> _lazyLength;
        private Lazy<DateTimeOffset> _lazyLastModified;

        public EmbeddedFileInfo(Assembly assembly, string resourcePath, string fileName)
        {
            _assembly = assembly;
            _resourcePath = resourcePath;

            _lazyLastModified = new Lazy<DateTimeOffset>(() =>
            {
                try
                {
                    // try and get the last modified time by looking at the last write time of the assembly.
#if !NETSTANDARD1_3
                    var fileInfo = new System.IO.FileInfo(assembly.Location);
                    return fileInfo.LastWriteTime;
#else
                    return DateTime.MinValue;
#endif

                }
                catch (Exception)
                {
                    // otherwise default to now.
                    return DateTimeOffset.UtcNow;
                }
            });

            IsDirectory = false;
            Name = fileName;
            PhysicalPath = null;
            Exists = true;

            _lazyLength = new Lazy<long>(() =>
            {
                using (var resourceStream = _assembly.GetManifestResourceStream(_resourcePath))
                {
                    return resourceStream.Length;
                }
            });
        }

        public Stream CreateReadStream()
        {
            var resourceStream = _assembly.GetManifestResourceStream(_resourcePath);
            return resourceStream;
        }

        public bool Exists { get; }
        public long Length { get { return _lazyLength.Value; } }
        public string PhysicalPath { get; }
        public string Name { get; }
        public DateTimeOffset LastModified { get { return _lazyLastModified.Value; } }
        public bool IsDirectory { get; }

    }
}
