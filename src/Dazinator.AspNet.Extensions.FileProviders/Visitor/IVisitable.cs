using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dazinator.AspNet.Extensions.FileProviders.Directory
{
    public interface IVisitable<T>
    {
        void Accept(T Visitor);
    }
}
