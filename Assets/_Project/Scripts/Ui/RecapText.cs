using System;
using System.Collections.Generic;
using System.Text;
using Moonbrew.Core;

namespace Moonbrew.Ui
{
    public static class RecapText
    {
        // One line per inventory, e.g. "Sac : +12 Blé, -3 Farine", in the order the entries come.
        // Names fall back to the IDs when unknown. Returns emptyText when nothing changed.
        public static string Lines(DayRecap recap, Func<string, string> inventoryName, Func<string, string> itemName, string lineFormat, string emptyText)
        {
            if (inventoryName == null || itemName == null || lineFormat == null)
            {
                throw new ArgumentNullException(inventoryName == null ? nameof(inventoryName) : itemName == null ? nameof(itemName) : nameof(lineFormat));
            }

            if (recap == null || recap.entries == null || recap.entries.Count == 0)
            {
                return emptyText;
            }

            var order = new List<string>();
            var items = new Dictionary<string, StringBuilder>();
            foreach (var entry in recap.entries)
            {
                if (!items.TryGetValue(entry.inventoryId, out var line))
                {
                    line = new StringBuilder();
                    items[entry.inventoryId] = line;
                    order.Add(entry.inventoryId);
                }

                if (line.Length > 0)
                {
                    line.Append(", ");
                }

                line.Append(entry.delta > 0 ? "+" : string.Empty).Append(entry.delta).Append(' ').Append(itemName(entry.itemId) ?? entry.itemId);
            }

            var text = new StringBuilder();
            foreach (var inventoryId in order)
            {
                if (text.Length > 0)
                {
                    text.Append('\n');
                }

                text.AppendFormat(lineFormat, inventoryName(inventoryId) ?? inventoryId, items[inventoryId]);
            }

            return text.ToString();
        }
    }
}
