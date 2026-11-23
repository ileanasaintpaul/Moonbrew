using Moonbrew.Core;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Cafe
{
    // The café's big stock, on a piece of kitchen furniture: a click from close by opens its panel,
    // and the panel's requests move items between the fridge and the owner's café inventory.
    public class CafeFridge : MonoBehaviour
    {
        [SerializeField] private CafeConfig config;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private InventoryHolder fridge;
        [SerializeField] private InventoryHolder ownerInventory;
        [SerializeField] private Transform player;

        private bool _subscribed;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(CafeConfig cafeConfig, EventBusAsset bus, InventoryHolder stock, InventoryHolder owner, Transform playerTransform)
        {
            config = cafeConfig;
            eventBus = bus;
            fridge = stock;
            ownerInventory = owner;
            player = playerTransform;
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<TargetClicked>(OnTargetClicked);
            eventBus.Subscribe<FridgeMoveRequested>(OnMoveRequested);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<TargetClicked>(OnTargetClicked);
            eventBus.Unsubscribe<FridgeMoveRequested>(OnMoveRequested);
            _subscribed = false;
        }

        private void OnTargetClicked(TargetClicked message)
        {
            if (message.Target == null || !message.Target.transform.IsChildOf(transform) || player == null)
            {
                return;
            }

            var offset = player.position - transform.position;
            offset.y = 0f;
            if (offset.magnitude <= config.FridgeReach)
            {
                eventBus.Publish(new FridgeOpened());
            }
        }

        private void OnMoveRequested(FridgeMoveRequested message)
        {
            var from = message.FromFridge ? fridge.Inventory : ownerInventory.Inventory;
            var to = message.FromFridge ? ownerInventory.Inventory : fridge.Inventory;
            if (message.Slot < 0 || message.Slot >= from.SlotCount)
            {
                return;
            }

            from.MoveSlot(message.Slot, to, message.WholeStack ? from.Slots[message.Slot].Quantity : 1);
        }
    }
}
