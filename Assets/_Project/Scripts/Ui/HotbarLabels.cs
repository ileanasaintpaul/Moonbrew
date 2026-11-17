using Moonbrew.Inventory;

namespace Moonbrew.Ui
{
    public static class HotbarLabels
    {
        // Slot 0 is key "1"; the tenth slot is key "0", like the number row.
        public static string Key(int index)
        {
            return index < 0 ? string.Empty : ((index + 1) % 10).ToString();
        }

        // A single item shows no number, like most farming games.
        public static string Count(ItemStack stack)
        {
            return stack.IsEmpty || stack.Quantity == 1 ? string.Empty : stack.Quantity.ToString();
        }
    }
}
