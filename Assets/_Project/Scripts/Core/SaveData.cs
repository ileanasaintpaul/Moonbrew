using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Core
{
    // One game's save file. Plain serializable data for JsonUtility, hence public fields.
    // Content is stored by stable ID, never by Unity reference.
    // Changing this format: bump CurrentVersion, add a step in SaveSerializer.Migrate and a test loading the old version.
    [Serializable]
    public class SaveData
    {
        // 1: day, inventories, recap. 2: farm plots and cut grass. 3: watering can. 4: stations. 5: learned recipes. 6: station time.
        // 7: the owner's name and look. 8: the look made of modular parts, with body shape.
        public const int CurrentVersion = 8;

        public int version = CurrentVersion;
        public string slotId;

        // A game created but not yet played to the end of a day: modules give it their starting state.
        public bool isNew;

        // The day the player wakes up to.
        public int day;

        // UTC, ISO 8601: sorts the games by last played.
        public string lastPlayedUtc;

        // Chosen when the game is created.
        public string ownerName = "";

        public AppearanceSave appearance = new AppearanceSave();

        public List<InventorySave> inventories = new List<InventorySave>();

        // Tilled cells of the farm and what grows on them.
        public List<PlotSave> plots = new List<PlotSave>();

        // IDs of the grass clumps cut with the sickle.
        public List<string> cutGrass = new List<string>();

        // Waterings used since the can was last filled; 0 is a full can.
        public int wateringCanUsed;

        // What each processing station (the mill...) holds.
        public List<StationSave> stations = new List<StationSave>();

        // IDs of the recipes learned during the game; those known from the start are not listed.
        public List<string> learnedRecipes = new List<string>();

        public StationSave FindStation(string id)
        {
            return stations.Find(station => station.id == id);
        }

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

        // The save written when a day ends: a copy of the day in which modules replace their own state
        // and add their part of the recap; the player wakes on the next day.
        public SaveData ForNextDay(int endedDay)
        {
            if (endedDay < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(endedDay), endedDay, "Days start at 1.");
            }

            var next = CopyForToday();
            next.day = endedDay + 1;
            next.recap.day = endedDay;
            return next;
        }

        // The day carried to another scene, never written to disk: that scene opens without a recap.
        public SaveData ForSameDay()
        {
            return CopyForToday();
        }

        // A recap without a day was gathered in the other scenes of today and is kept; a dated one is yesterday's.
        private SaveData CopyForToday()
        {
            var copy = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(this));
            copy.isNew = false;
            copy.recap ??= new DayRecap();
            if (copy.recap.day > 0)
            {
                copy.recap = new DayRecap();
            }

            return copy;
        }

        public InventorySave FindInventory(string id)
        {
            return inventories.Find(inventory => inventory.id == id);
        }
    }

    // The owner's look by part and colour ID; an empty ID means none, or the default for the top, the bottom and the colours.
    // The body shape goes from -100 to 100: masculine to feminine, slim to heavy, lean to muscular.
    [Serializable]
    public class AppearanceSave
    {
        public string topId = "";
        public string bottomId = "";
        public string hairId = "";
        public string browsId = "";
        public string beardId = "";
        public string hatId = "";
        public int bodyType;
        public int bodySize;
        public int musculature;
        public string skinColourId = "";
        public string hairColourId = "";
        public string browColourId = "";
        public string beardColourId = "";
        public string topColourId = "";
        public string bottomColourId = "";
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

    // A station's batch; processId is empty when the station is idle.
    [Serializable]
    public class StationSave
    {
        public string id;
        public string processId;
        public int batches;
        public int minutesLeft;

        // Time of day the station last heard, -1 for none: a batch keeps running while the player is in another scene.
        public int minutes = -1;
    }

    [Serializable]
    public class DayRecap
    {
        public int day;
        public List<RecapEntry> entries = new List<RecapEntry>();

        // Adds a change, merged with the one already noted for the same item in the same inventory.
        public void Add(string inventoryId, string itemId, int delta)
        {
            var entry = entries.Find(e => e.inventoryId == inventoryId && e.itemId == itemId);
            if (entry == null)
            {
                entries.Add(new RecapEntry { inventoryId = inventoryId, itemId = itemId, delta = delta });
            }
            else
            {
                entry.delta += delta;
            }

            entries.RemoveAll(e => e.delta == 0);
        }
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
