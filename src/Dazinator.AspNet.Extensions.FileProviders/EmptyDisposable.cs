using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    internal class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new EmptyDisposable();

        private EmptyDisposable()
        {
        }

        public void Dispose()
        {
        }
    }
}
