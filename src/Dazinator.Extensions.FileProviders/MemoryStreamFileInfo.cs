using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace Dazinator.Extensions.FileProviders
{
    public class MemoryStreamFileInfo : IFileInfo
    {
        private readonly MemoryStream _stream;
        private Lazy<long> _lazyLength;

        private Object _lock = new object();
        public MemoryStreamFileInfo(MemoryStream contents, string name)
        {
            _stream = contents;
            LastModified = DateTimeOffset.UtcNow;
            IsDirectory = false;
            Name = name;
            PhysicalPath = null;
            Exists = true;

            _lazyLength = new Lazy<long>(() =>
            {
                return _stream.Length;
            });
        }

        public Stream CreateReadStream()
        {
            var stream = new MemoryStream((int)_stream.Length);
            lock (_lock) // in case concurrent threads try to read the same file, we want to lock.
            {
                _stream.Position = 0;
                _stream.CopyTo(stream);
                stream.Position = 0;
                _stream.Position = 0;
            }
            return stream;
        }

        public bool Exists { get; }
        public long Length { get { return _lazyLength.Value; } }
        public string PhysicalPath { get; }
        public string Name { get; }
        public DateTimeOffset LastModified { get; }
        public bool IsDirectory { get; }


    }
}
