using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    /// <summary>
    /// Represents a composition of <see cref="IChangeToken"/>.
    /// </summary>
    internal class CompositeChangeToken : IChangeToken
    {
        private readonly IList<IChangeToken> _changeTokens;

        /// <summary>
        /// Creates a new instance of <see cref="CompositeChangeToken"/>.
        /// </summary>
        /// <param name="changeTokens">The list of <see cref="IChangeToken"/> to compose.</param>
        public CompositeChangeToken(IList<IChangeToken> changeTokens)
        {
            if (changeTokens == null)
            {
                throw new ArgumentNullException(nameof(changeTokens));
            }
            _changeTokens = changeTokens;
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            var disposables = new List<IDisposable>();
            for (var i = 0; i < _changeTokens.Count; i++)
            {
                var changeToken = _changeTokens[i];
                var disposable = _changeTokens[i].RegisterChangeCallback(callback, state);
                disposables.Add(disposable);
            }
            return new CompositeDisposable(disposables);
        }

        public bool HasChanged
        {
            get { return _changeTokens.Any(token => token.HasChanged); }
        }

        public bool ActiveChangeCallbacks
        {
            get { return _changeTokens.Any(token => token.ActiveChangeCallbacks); }
        }
    }
}
