using System;
using System.Collections.Generic;

namespace Moonbrew.Core
{
    // Typed publish/subscribe between modules. Event types live in Core so every module can share them
    // without referencing each other.
    public class EventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handlers.TryGetValue(typeof(TEvent), out var existing);
            _handlers[typeof(TEvent)] = Delegate.Combine(existing, handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!_handlers.TryGetValue(typeof(TEvent), out var existing))
            {
                return;
            }

            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
            {
                _handlers.Remove(typeof(TEvent));
            }
            else
            {
                _handlers[typeof(TEvent)] = remaining;
            }
        }

        // Handlers run in subscription order. Delegates are immutable, so a handler that subscribes or
        // unsubscribes during a publish only affects the next publish.
        public void Publish<TEvent>(TEvent message)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                ((Action<TEvent>)handlers).Invoke(message);
            }
        }

        public void Clear()
        {
            _handlers.Clear();
        }
    }
}
