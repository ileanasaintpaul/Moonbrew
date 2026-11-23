using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Inventory
{
    // A place to empty the bag's ingredients into a stock: the farm's shipping crate, the café's pantry.
    public class IngredientDrop : MonoBehaviour
    {
        [SerializeField] private InventoryHolder bag;
        [SerializeField] private InventoryHolder stock;
        [SerializeField] private Transform player;
        [SerializeField] private EventBusAsset eventBus;

        private bool _subscribed;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(InventoryHolder playerBag, InventoryHolder target, Transform playerTransform, EventBusAsset bus)
        {
            bag = playerBag;
            stock = target;
            player = playerTransform;
            eventBus = bus;
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<TargetClicked>(OnTargetClicked);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<TargetClicked>(OnTargetClicked);
            _subscribed = false;
        }

        private void OnTargetClicked(TargetClicked message)
        {
            if (message.Target == null || !message.Target.transform.IsChildOf(transform) || player == null || bag == null || stock == null)
            {
                return;
            }

            var offset = player.position - transform.position;
            offset.y = 0f;
            if (offset.magnitude <= stock.Config.DropReach)
            {
                var moved = IngredientTransfer.MoveAll(bag.Inventory, stock.Inventory);
                Debug.Log($"[Inventory] Dropped {moved} item(s) into {stock.Config.Id}");
            }
        }
    }
}
