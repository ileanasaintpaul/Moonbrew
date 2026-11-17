using System;
using UnityEngine;

namespace Moonbrew.Inventory
{
    // Serializable so starting items can be set in the inspector.
    [Serializable]
    public struct ItemStack
    {
        [SerializeField] private ItemData item;
        [SerializeField, Min(0)] private int quantity;

        public ItemStack(ItemData item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }

        public ItemData Item => item;

        public int Quantity => quantity;

        public bool IsEmpty => item == null || quantity <= 0;
    }
}
