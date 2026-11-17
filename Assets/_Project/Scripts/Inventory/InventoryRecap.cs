using System.Collections.Generic;
using Moonbrew.Core;

namespace Moonbrew.Inventory
{
    public static class InventoryRecap
    {
        // What an inventory gained and lost over the day, item by item. Unchanged items are left out.
        public static List<RecapEntry> Between(string inventoryId, IReadOnlyDictionary<string, int> morning, IReadOnlyDictionary<string, int> evening)
        {
            var entries = new List<RecapEntry>();
            morning ??= new Dictionary<string, int>();
            evening ??= new Dictionary<string, int>();

            foreach (var pair in evening)
            {
                morning.TryGetValue(pair.Key, out var before);
                Add(entries, inventoryId, pair.Key, pair.Value - before);
            }

            foreach (var pair in morning)
            {
                if (!evening.ContainsKey(pair.Key))
                {
                    Add(entries, inventoryId, pair.Key, -pair.Value);
                }
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.itemId, b.itemId));
            return entries;
        }

        private static void Add(List<RecapEntry> entries, string inventoryId, string itemId, int delta)
        {
            if (delta != 0)
            {
                entries.Add(new RecapEntry { inventoryId = inventoryId, itemId = itemId, delta = delta });
            }
        }
    }
}
