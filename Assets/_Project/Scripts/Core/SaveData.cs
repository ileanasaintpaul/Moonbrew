using System;
using System.Collections.Generic;

namespace Moonbrew.Core
{
    // One game's save file. Plain serializable data for JsonUtility, hence public fields.
    // Content is stored by stable ID, never by Unity reference.
    // Changing this format: bump CurrentVersion, add a step in SaveSerializer.Migrate and a test loading the old version.
    [Serializable]
    public class SaveData
    {
        // 1: day, inventories, recap. 2: farm plots and cut grass.
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public string slotId;

        // A game created but not yet played to the end of a day: modules give it their starting state.
        public bool isNew;

        // The day the player wakes up to.
        public int day;

        // UTC, ISO 8601: sorts the games by last played.
        public string lastPlayedUtc;

        public List<InventorySave> inventories = new List<InventorySave>();

        // Tilled cells of the farm and what grows on them.
        public List<PlotSave> plots = new List<PlotSave>();

        // IDs of the grass clumps cut with the sickle.
        public List<string> cutGrass = new List<string>();

        // Summary of the previous day, shown when this day opens. JsonUtility never writes null, so day 0 means none.
        public DayRecap recap = new DayRecap();

        public bool HasRecap => recap != null && recap.day > 0;

        public static SaveData CreateNew(string slotId, int firstDay)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                throw new ArgumentException("A game needs a slot ID.", nameof(slotId));
            }

            if (firstDay < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(firstDay), firstDay, "Days start at 1.");
            }

            return new SaveData { slotId = slotId, day = firstDay, isNew = true };
        }

        // The empty save written when a day ends: modules fill their state and the recap, the player wakes on the next day.
        public SaveData ForNextDay(int endedDay)
        {
            if (endedDay < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(endedDay), endedDay, "Days start at 1.");
            }

            return new SaveData { slotId = slotId, day = endedDay + 1, recap = new DayRecap { day = endedDay } };
        }

        public InventorySave FindInventory(string id)
        {
            return inventories.Find(inventory => inventory.id == id);
        }
    }

    [Serializable]
    public class InventorySave
    {
        public string id;
        public List<SlotSave> slots = new List<SlotSave>();
    }

    [Serializable]
    public class SlotSave
    {
        public int index;
        public string itemId;
        public int quantity;
    }

    // A tilled farm cell; cropId is empty when nothing is planted.
    [Serializable]
    public class PlotSave
    {
        public int x;
        public int y;
        public string cropId;
        public int daysGrown;
    }

    [Serializable]
    public class DayRecap
    {
        public int day;
        public List<RecapEntry> entries = new List<RecapEntry>();
    }

    // How much an item's total changed in an inventory over the day.
    [Serializable]
    public class RecapEntry
    {
        public string inventoryId;
        public string itemId;
        public int delta;
    }
}
