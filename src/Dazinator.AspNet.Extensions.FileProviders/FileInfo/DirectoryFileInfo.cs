using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Dazinator.AspNet.Extensions.FileProviders
{

  public class DirectoryFileInfo : IFileInfo
    {
        public DirectoryFileInfo(string name)
        {
            Name = name;
        }
        public Stream CreateReadStream()
        {
            throw new InvalidOperationException("Cannot create a stream for a directory.");
        }

        public bool Exists { get; } = true;
        public long Length { get; } = -1;
        public string PhysicalPath { get; } = null;
        public string Name { get; }
        public DateTimeOffset LastModified { get; }
        public bool IsDirectory => true;
    }
}
