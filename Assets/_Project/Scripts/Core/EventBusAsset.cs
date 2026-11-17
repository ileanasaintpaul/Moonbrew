using System;
using UnityEngine;

namespace Moonbrew.Core
{
    // The shared event bus as an asset: components get it through a serialized reference instead of a singleton.
    [CreateAssetMenu(menuName = "Moonbrew/Core/Event Bus", fileName = "SO_EventBus")]
    public class EventBusAsset : ScriptableObject
    {
        private readonly EventBus _bus = new EventBus();

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            _bus.Subscribe(handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            _bus.Unsubscribe(handler);
        }

        public void Publish<TEvent>(TEvent message)
        {
            _bus.Publish(message);
        }

        // Assets outlive Play mode in the editor: forgotten subscriptions must not leak into the next session.
        private void OnDisable()
        {
            _bus.Clear();
        }
    }
}
