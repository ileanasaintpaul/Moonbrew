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

            // Never saved yet, say the café's inventory on the first day the game was started on the farm.
            var saved = message.Data.FindInventory(config.Id);
            if (saved == null)
            {
                OnNewGame(new NewGameStarted());
                return;
            }

            var skipped = Inventory.Restore(saved, catalog.Find);
            if (skipped > 0)
            {
                Debug.LogWarning($"[Inventory] {config.Id}: {skipped} saved slot(s) could not be restored.");
            }

            _morningTotals = Inventory.Totals();
        }

        private void OnSaving(GameSaving message)
        {
            // The save may already hold this inventory and part of its recap, carried from another scene of the same day.
            message.Data.inventories.RemoveAll(inventory => inventory.id == config.Id);
            message.Data.inventories.Add(Inventory.ToSave(config.Id));
            var totals = Inventory.Totals();
            foreach (var entry in InventoryRecap.Between(config.Id, _morningTotals, totals))
            {
                message.Data.recap.Add(entry.inventoryId, entry.itemId, entry.delta);
            }

            _morningTotals = totals;
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
