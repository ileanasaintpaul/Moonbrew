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

        // How full a gauge is, from 0 to 100.
        public static float FillPercent(int value, int capacity)
        {
            if (capacity <= 0)
            {
                return 0f;
            }

            return 100f * System.Math.Max(0, System.Math.Min(value, capacity)) / capacity;
        }

        // A single item shows no number, like most farming games.
        public static string Count(ItemStack stack)
        {
            return stack.IsEmpty || stack.Quantity == 1 ? string.Empty : stack.Quantity.ToString();
        }
    }
}
