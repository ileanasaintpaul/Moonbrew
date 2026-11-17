using System.Collections.Generic;
using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Inventory
{
    // Owns one inventory in the scene (the player's bag or the café stock): announces its changes on the bus,
    // gets its starting items in a new game, and saves, restores and sums up its day through the session events.
    public class InventoryHolder : MonoBehaviour
    {
        [SerializeField] private InventoryConfig config;
        [SerializeField] private ItemCatalog catalog;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private ItemStack[] startingItems = new ItemStack[0];

        private Dictionary<string, int> _morningTotals = new Dictionary<string, int>();
        private bool _subscribed;

        public ItemInventory Inventory { get; private set; }

        public InventoryConfig Config => config;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(InventoryConfig inventoryConfig, ItemCatalog itemCatalog, EventBusAsset bus, ItemStack[] items = null)
        {
            config = inventoryConfig;
            catalog = itemCatalog;
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
            if (Inventory == null)
            {
                return;
            }

            Inventory.Changed += OnChanged;
            if (eventBus != null && !_subscribed)
            {
                eventBus.Subscribe<NewGameStarted>(OnNewGame);
                eventBus.Subscribe<GameLoaded>(OnLoaded);
                eventBus.Subscribe<GameSaving>(OnSaving);
                _subscribed = true;
            }
        }

        private void OnDisable()
        {
            if (Inventory != null)
            {
                Inventory.Changed -= OnChanged;
            }

            if (_subscribed)
            {
                eventBus.Unsubscribe<NewGameStarted>(OnNewGame);
                eventBus.Unsubscribe<GameLoaded>(OnLoaded);
                eventBus.Unsubscribe<GameSaving>(OnSaving);
                _subscribed = false;
            }
        }

        private void OnNewGame(NewGameStarted message)
        {
            foreach (var stack in startingItems)
            {
                if (!stack.IsEmpty)
                {
                    Inventory.Add(stack.Item, stack.Quantity);
                }
            }

            _morningTotals = Inventory.Totals();
        }

        private void OnLoaded(GameLoaded message)
        {
            if (catalog == null)
            {
                Debug.LogError($"{nameof(InventoryHolder)} on {name} needs the {nameof(ItemCatalog)} to load a save.", this);
                return;
            }

            var skipped = Inventory.Restore(message.Data.FindInventory(config.Id), catalog.Find);
            if (skipped > 0)
            {
                Debug.LogWarning($"[Inventory] {config.Id}: {skipped} saved slot(s) could not be restored.");
            }

            _morningTotals = Inventory.Totals();
        }

        private void OnSaving(GameSaving message)
        {
            message.Data.inventories.Add(Inventory.ToSave(config.Id));
            message.Data.recap.entries.AddRange(InventoryRecap.Between(config.Id, _morningTotals, Inventory.Totals()));
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
