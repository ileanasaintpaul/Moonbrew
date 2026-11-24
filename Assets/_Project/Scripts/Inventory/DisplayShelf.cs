using System.Collections.Generic;
using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Inventory
{
    // Shows an inventory's content in the scene: each slot sets out its item's world model at its own spot,
    // a few side by side when the slot holds several (the slices of a cake in the display case).
    public class DisplayShelf : MonoBehaviour
    {
        [SerializeField] private InventoryHolder stock;
        [SerializeField] private EventBusAsset eventBus;
        [Tooltip("One spot per slot, in slot order.")]
        [SerializeField] private Transform[] slotSpots = new Transform[0];
        [Tooltip("Most models shown for one slot, side by side.")]
        [SerializeField, Min(1)] private int maxShownPerSlot = 3;
        [Tooltip("Gap between two models of the same slot, in meters, along the spot's right.")]
        [SerializeField, Min(0f)] private float spacing = 0.14f;

        private List<GameObject>[] _models;
        private ItemData[] _items;
        private int[] _counts;
        private bool _subscribed;

        public int ShownCount(int slot) => _models != null && slot < _models.Length ? _models[slot].Count : 0;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(InventoryHolder holder, EventBusAsset bus, Transform[] spots)
        {
            stock = holder;
            eventBus = bus;
            slotSpots = spots ?? new Transform[0];
        }

        // How many models a slot shows for this quantity.
        public static int Shown(int quantity, int max)
        {
            return Mathf.Clamp(quantity, 0, Mathf.Max(0, max));
        }

        private void Awake()
        {
            _models = new List<GameObject>[slotSpots.Length];
            _items = new ItemData[slotSpots.Length];
            _counts = new int[slotSpots.Length];
            for (var i = 0; i < _models.Length; i++)
            {
                _models[i] = new List<GameObject>();
            }
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<InventoryChanged>(OnInventoryChanged);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<InventoryChanged>(OnInventoryChanged);
            _subscribed = false;
        }

        private void Start()
        {
            Refresh();
        }

        private void OnInventoryChanged(InventoryChanged message)
        {
            if (stock != null && stock.Config != null && message.InventoryId == stock.Config.Id)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (stock == null || stock.Inventory == null)
            {
                return;
            }

            for (var i = 0; i < slotSpots.Length && i < stock.Inventory.SlotCount; i++)
            {
                var slot = stock.Inventory.Slots[i];
                var item = slot.IsEmpty ? null : slot.Item;
                var count = item != null && item.WorldModel != null ? Shown(slot.Quantity, maxShownPerSlot) : 0;
                if (item == _items[i] && count == _counts[i])
                {
                    continue;
                }

                foreach (var model in _models[i])
                {
                    Destroy(model);
                }

                _models[i].Clear();
                _items[i] = item;
                _counts[i] = count;
                for (var n = 0; n < count; n++)
                {
                    var spot = slotSpots[i];
                    var offset = (n - (count - 1) / 2f) * spacing;
                    var model = Instantiate(item.WorldModel, spot.position + spot.right * offset, spot.rotation, spot);
                    // The case's own shelves would leave them in the dark.
                    foreach (var meshRenderer in model.GetComponentsInChildren<Renderer>())
                    {
                        meshRenderer.receiveShadows = false;
                    }

                    foreach (var solid in model.GetComponentsInChildren<Collider>())
                    {
                        solid.enabled = false;
                    }

                    _models[i].Add(model);
                }
            }
        }
    }
}
