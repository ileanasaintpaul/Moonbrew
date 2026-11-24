using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Inventory
{
    // Overnight, the shipping crate's content reaches the café fridge; what does not fit waits in the crate.
    public class OvernightDelivery : MonoBehaviour
    {
        [SerializeField] private InventoryHolder crate;
        [SerializeField] private InventoryHolder destination;
        [SerializeField] private EventBusAsset eventBus;

        private bool _subscribed;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(InventoryHolder shippingCrate, InventoryHolder target, EventBusAsset bus)
        {
            crate = shippingCrate;
            destination = target;
            eventBus = bus;
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<NightPassed>(OnNightPassed);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<NightPassed>(OnNightPassed);
            _subscribed = false;
        }

        private void OnNightPassed(NightPassed message)
        {
            if (crate != null && destination != null)
            {
                IngredientTransfer.MoveAll(crate.Inventory, destination.Inventory, true);
            }
        }
    }
}
