using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Dazinator.AspNet.Extensions.FileProviders
{
    public class InMemoryChangeToken : IChangeToken
    {

        // public StringFileInfo File { get; set; }

        public InMemoryChangeToken()
        {
            //  File = file;
        }

        private readonly ConcurrentBag<Tuple<Action<object>, object, IDisposable>> _callbacks = new ConcurrentBag<Tuple<Action<object>, object, IDisposable>>();

        public bool ActiveChangeCallbacks { get; set; }

        public bool HasChanged { get; set; }

        public ConcurrentBag<Tuple<Action<object>, object, IDisposable>> Callbacks
        {
            get
            {
                return _callbacks;
            }
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            var disposable = EmptyDisposable.Instance;
            _callbacks.Add(Tuple.Create(callback, state, (IDisposable)disposable));
            return disposable;
        }

        public void RaiseCallback(object newItem)
        {
            foreach (var callback in _callbacks)
            {
                callback.Item1(newItem);
            }
        }
    }
}
