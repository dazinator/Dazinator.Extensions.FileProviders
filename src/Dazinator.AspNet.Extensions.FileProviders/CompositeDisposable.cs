using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    /// <summary>
    /// Represents a composition of <see cref="IDisposable"/>.
    /// </summary>
    internal class CompositeDisposable : IDisposable
    {
        private readonly IList<IDisposable> _disposables;
        /// <summary>
        /// Creates a new instance of <see cref="CompositeDisposable"/>.
        /// </summary>
        /// <param name="disposables">The list of <see cref="IDisposable"/> to compose.</param>
        public CompositeDisposable(IList<IDisposable> disposables)
        {
            if (disposables == null)
            {
                throw new ArgumentNullException(nameof(disposables));
            }
            _disposables = disposables;
        }

        public void Dispose()
        {
            for (var i = 0; i < _disposables.Count; i++)
            {
                _disposables[i].Dispose();
            }
        }
    }
}
