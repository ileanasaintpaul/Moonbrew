using System;
using System.Collections.Generic;
using Moonbrew.Core;

namespace Moonbrew.Inventory
{
    // Slots holding stacks of one item each, up to the item's MaxStack.
    public class ItemInventory
    {
        private readonly ItemStack[] _slots;

        public ItemInventory(int slotCount)
        {
            if (slotCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount), "An inventory needs at least one slot.");
            }

            _slots = new ItemStack[slotCount];
        }

        // The item whose total changed, and its new total.
        public event Action<ItemData, int> Changed;

        public IReadOnlyList<ItemStack> Slots => _slots;

        public int SlotCount => _slots.Length;

        public int Count(ItemData item)
        {
            if (item == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var slot in _slots)
            {
                if (slot.Item == item)
                {
                    total += slot.Quantity;
                }
            }

            return total;
        }

        // How many of this item still fit, filling partial stacks first, then empty slots.
        public int SpaceFor(ItemData item)
        {
            if (item == null)
            {
                return 0;
            }

            var space = 0;
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty)
                {
                    space += item.MaxStack;
                }
                else if (slot.Item == item)
                {
                    space += item.MaxStack - slot.Quantity;
                }
            }

            return space;
        }

        // Adds as many as fit and returns that number; the caller decides what to do with the rest.
        public int Add(ItemData item, int quantity)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var remaining = Math.Min(quantity, SpaceFor(item));
            if (remaining <= 0)
            {
                return 0;
            }

            var added = remaining;
            for (var i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (_slots[i].Item == item)
                {
                    remaining -= Fill(i, item, remaining);
                }
            }

            for (var i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    remaining -= Fill(i, item, remaining);
                }
            }

            Changed?.Invoke(item, Count(item));
            return added;
        }

        // All or nothing: a recipe never consumes half its ingredients.
        public bool Remove(ItemData item, int quantity)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (quantity <= 0 || Count(item) < quantity)
            {
                return false;
            }

            var remaining = quantity;
            for (var i = _slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                if (_slots[i].Item != item)
                {
                    continue;
                }

                var taken = Math.Min(remaining, _slots[i].Quantity);
                var left = _slots[i].Quantity - taken;
                _slots[i] = left > 0 ? new ItemStack(item, left) : default;
                remaining -= taken;
            }

            Changed?.Invoke(item, Count(item));
            return true;
        }

        // Moves up to quantity from one slot into another inventory; what does not fit stays in the slot.
        // Returns how many moved.
        public int MoveSlot(int slot, ItemInventory target, int quantity)
        {
            if (target == null || target == this || slot < 0 || slot >= _slots.Length || quantity <= 0 || _slots[slot].IsEmpty)
            {
                return 0;
            }

            var item = _slots[slot].Item;
            var moved = target.Add(item, Math.Min(quantity, _slots[slot].Quantity));
            if (moved <= 0)
            {
                return 0;
            }

            var left = _slots[slot].Quantity - moved;
            _slots[slot] = left > 0 ? new ItemStack(item, left) : default;
            Changed?.Invoke(item, Count(item));
            return moved;
        }

        // Totals per item ID, for the end-of-day recap. An item without ID cannot be saved, so it is left out
        // (DataValidationTests makes sure real items have one).
        public Dictionary<string, int> Totals()
        {
            var totals = new Dictionary<string, int>();
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty && !string.IsNullOrEmpty(slot.Item.Id))
                {
                    totals.TryGetValue(slot.Item.Id, out var total);
                    totals[slot.Item.Id] = total + slot.Quantity;
                }
            }

            return totals;
        }

        public InventorySave ToSave(string inventoryId)
        {
            var save = new InventorySave { id = inventoryId };
            for (var i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    save.slots.Add(new SlotSave { index = i, itemId = _slots[i].Item.Id, quantity = _slots[i].Quantity });
                }
            }

            return save;
        }

        // Replaces the contents with a save. Slots whose item no longer exists, whose index is out of range
        // or whose quantity is invalid are skipped; their number is returned so the caller can report them.
        public int Restore(InventorySave save, Func<string, ItemData> findItem)
        {
            if (findItem == null)
            {
                throw new ArgumentNullException(nameof(findItem));
            }

            var changed = new HashSet<ItemData>();
            for (var i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    changed.Add(_slots[i].Item);
                }

                _slots[i] = default;
            }

            var skipped = 0;
            foreach (var slot in save?.slots ?? new List<SlotSave>())
            {
                var item = findItem(slot.itemId);
                if (item == null || slot.index < 0 || slot.index >= _slots.Length || slot.quantity < 1 || !_slots[slot.index].IsEmpty)
                {
                    skipped++;
                    continue;
                }

                _slots[slot.index] = new ItemStack(item, Math.Min(slot.quantity, item.MaxStack));
                changed.Add(item);
            }

            foreach (var item in changed)
            {
                Changed?.Invoke(item, Count(item));
            }

            return skipped;
        }

        private int Fill(int index, ItemData item, int quantity)
        {
            var current = _slots[index].Quantity;
            var put = Math.Min(quantity, item.MaxStack - current);
            _slots[index] = new ItemStack(item, current + put);
            return put;
        }
    }
}
