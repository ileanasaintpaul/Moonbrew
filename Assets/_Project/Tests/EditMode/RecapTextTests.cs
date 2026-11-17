using System;
using Moonbrew.Core;
using Moonbrew.Ui;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class RecapTextTests
    {
        private const string Line = "{0} : {1}";
        private const string Empty = "Rien n'a changé.";

        [Test]
        public void Lines_SeveralInventories_OneLinePerInventory()
        {
            var recap = new DayRecap { day = 1 };
            recap.entries.Add(new RecapEntry { inventoryId = "inventory_player", itemId = "crop_wheat", delta = 12 });
            recap.entries.Add(new RecapEntry { inventoryId = "inventory_cafe", itemId = "dish_wheat_cake", delta = -2 });
            recap.entries.Add(new RecapEntry { inventoryId = "inventory_player", itemId = "seed_wheat", delta = -3 });

            var text = RecapText.Lines(recap, InventoryName, ItemName, Line, Empty);

            Assert.AreEqual("Sac : +12 Blé, -3 Graines de blé\nStock du café : -2 Gâteau au blé", text);
        }

        [Test]
        public void Lines_UnknownNames_FallBackToIds()
        {
            var recap = new DayRecap { day = 1 };
            recap.entries.Add(new RecapEntry { inventoryId = "inventory_x", itemId = "item_x", delta = 1 });

            Assert.AreEqual("inventory_x : +1 item_x", RecapText.Lines(recap, id => null, id => null, Line, Empty));
        }

        [Test]
        public void Lines_NoEntriesOrNullRecap_ShowsEmptyText()
        {
            Assert.AreEqual(Empty, RecapText.Lines(new DayRecap { day = 1 }, InventoryName, ItemName, Line, Empty));
            Assert.AreEqual(Empty, RecapText.Lines(null, InventoryName, ItemName, Line, Empty));
        }

        [Test]
        public void Lines_NullNameLookup_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => RecapText.Lines(new DayRecap(), null, ItemName, Line, Empty));
            Assert.Throws<ArgumentNullException>(() => RecapText.Lines(new DayRecap(), InventoryName, ItemName, null, Empty));
        }

        private static string InventoryName(string id)
        {
            return id == "inventory_player" ? "Sac" : id == "inventory_cafe" ? "Stock du café" : null;
        }

        private static string ItemName(string id)
        {
            switch (id)
            {
                case "crop_wheat": return "Blé";
                case "seed_wheat": return "Graines de blé";
                case "dish_wheat_cake": return "Gâteau au blé";
                default: return null;
            }
        }
    }
}
