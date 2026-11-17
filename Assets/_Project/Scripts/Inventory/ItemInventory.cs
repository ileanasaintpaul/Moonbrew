using System;
using System.Collections.Generic;

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

        private int Fill(int index, ItemData item, int quantity)
        {
            var current = _slots[index].Quantity;
            var put = Math.Min(quantity, item.MaxStack - current);
            _slots[index] = new ItemStack(item, current + put);
            return put;
        }
    }
}
