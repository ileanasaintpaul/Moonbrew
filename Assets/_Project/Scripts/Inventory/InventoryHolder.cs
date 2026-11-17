using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Inventory
{
    // Owns one inventory in the scene (the player's bag or the café stock) and announces its changes on the bus.
    public class InventoryHolder : MonoBehaviour
    {
        [SerializeField] private InventoryConfig config;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private ItemStack[] startingItems = new ItemStack[0];

        public ItemInventory Inventory { get; private set; }

        public InventoryConfig Config => config;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(InventoryConfig inventoryConfig, EventBusAsset bus, ItemStack[] items = null)
        {
            config = inventoryConfig;
            eventBus = bus;
            startingItems = items ?? new ItemStack[0];
        }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"{nameof(InventoryHolder)} on {name} has no {nameof(InventoryConfig)} assigned.", this);
                enabled = false;
                return;
            }

            Inventory = new ItemInventory(config.SlotCount);
        }

        private void OnEnable()
        {
            if (Inventory != null)
            {
                Inventory.Changed += OnChanged;
            }
        }

        private void OnDisable()
        {
            if (Inventory != null)
            {
                Inventory.Changed -= OnChanged;
            }
        }

        // In Start, so listeners subscribed in OnEnable hear the starting items.
        private void Start()
        {
            foreach (var stack in startingItems)
            {
                if (!stack.IsEmpty)
                {
                    Inventory.Add(stack.Item, stack.Quantity);
                }
            }
        }

        private void OnChanged(ItemData item, int quantity)
        {
            Debug.Log($"[Inventory] {config.Id}: {item.Id} = {quantity}");
            if (eventBus != null)
            {
                eventBus.Publish(new InventoryChanged(config.Id, item.Id, quantity));
            }
        }
    }
}
