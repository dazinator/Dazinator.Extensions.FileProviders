using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public class BaseDirectoryVisitor
    {
        public virtual void Visit(FileDirectoryItem item)
        {
            // no op.
        }

        public virtual void Visit(FolderDirectoryItem item)
        {
            // no op.
        }

    }
}

