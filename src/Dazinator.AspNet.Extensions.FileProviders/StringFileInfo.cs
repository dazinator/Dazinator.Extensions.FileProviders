using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.Extensions.FileProviders
{
    public class StringFileInfo : IFileInfo
    {
        private readonly string _contents;
        private Lazy<long> _lazyLength;

        private static Encoding DefaultEncoding = Encoding.UTF8;

        private readonly Encoding _Encoding;

        public StringFileInfo(string contents, string name, Encoding encoding = null)
        {
            _contents = contents;
            LastModified = DateTimeOffset.UtcNow;
            Name = name;
            _Encoding = encoding ?? DefaultEncoding;
            _lazyLength = new Lazy<long>(() =>
            {
                return _Encoding.GetByteCount(_contents);
            });
        }

        public Stream CreateReadStream()
        {
            return new MemoryStream(_Encoding.GetBytes(_contents ?? ""));
        }

        public bool Exists => true;
        public long Length { get { return _lazyLength.Value; } }
        public string PhysicalPath => null;
        public string Name { get; }
        public DateTimeOffset LastModified { get; }
        public bool IsDirectory => false;
    }
}
