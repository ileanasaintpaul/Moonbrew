namespace Moonbrew.Inventory
{
    public static class IngredientTransfer
    {
        // Moves every crop and ingredient that fits (or every item at all); tools, seeds and dishes stay otherwise. Returns how many items moved.
        public static int MoveAll(ItemInventory from, ItemInventory to, bool everything = false)
        {
            if (from == null || to == null || from == to)
            {
                return 0;
            }

            var moved = 0;
            for (var slot = 0; slot < from.SlotCount; slot++)
            {
                var stack = from.Slots[slot];
                if (!stack.IsEmpty && (everything || stack.Item.Category == ItemCategory.Crop || stack.Item.Category == ItemCategory.Ingredient))
                {
                    moved += from.MoveSlot(slot, to, stack.Quantity);
                }
            }

            return moved;
        }
    }
}
