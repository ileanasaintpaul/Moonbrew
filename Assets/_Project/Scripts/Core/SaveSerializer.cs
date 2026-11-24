using System;
using UnityEngine;

namespace Moonbrew.Core
{
    public static class SaveSerializer
    {
        public static string ToJson(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return JsonUtility.ToJson(data, true);
        }

        // Never throws on bad input: an unreadable save is reported through error.
        public static bool TryFromJson(string json, out SaveData data, out string error)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "The save is empty.";
                return false;
            }

            SaveData parsed;
            int version;
            try
            {
                // Read on its own: SaveData starts at CurrentVersion, which would hide a missing version.
                version = JsonUtility.FromJson<SaveHeader>(json)?.version ?? 0;
                parsed = JsonUtility.FromJson<SaveData>(json);
            }
            catch (ArgumentException exception)
            {
                error = $"The save is not valid JSON: {exception.Message}";
                return false;
            }

            if (parsed == null || version < 1)
            {
                error = "The save has no version.";
                return false;
            }

            if (version > SaveData.CurrentVersion)
            {
                error = $"The save comes from a newer version of the game ({version}).";
                return false;
            }

            if (string.IsNullOrEmpty(parsed.slotId) || parsed.day < 1)
            {
                error = "The save has no slot ID or day.";
                return false;
            }

            data = Migrate(parsed, version);
            error = null;
            return true;
        }

        [Serializable]
        private class SaveHeader
        {
            public int version;
        }

        // Brings an older save up to CurrentVersion, one version at a time.
        private static SaveData Migrate(SaveData data, int version)
        {
            // 1 -> 2: farm plots and cut grass were added; an older game has none.
            if (version < 2)
            {
                data.plots ??= new System.Collections.Generic.List<PlotSave>();
                data.cutGrass ??= new System.Collections.Generic.List<string>();
            }

            // 2 -> 3: the watering can was added; an older game starts with a full can.
            if (version < 3)
            {
                data.wateringCanUsed = 0;
            }

            // 3 -> 4: processing stations were added; an older game has them all idle.
            if (version < 4)
            {
                data.stations ??= new System.Collections.Generic.List<StationSave>();
            }

            // 4 -> 5: learned recipes were added; an older game knows only the starting recipes.
            if (version < 5)
            {
                data.learnedRecipes ??= new System.Collections.Generic.List<string>();
            }

            // 5 -> 6: stations note the time of day they last heard; an older save opens a morning, so none.
            if (version < 6)
            {
                data.stations.ForEach(station => station.minutes = -1);
            }

            // 6 -> 7: the owner's name and look were added; an older game keeps the default look and no name.
            if (version < 7)
            {
                data.ownerName ??= "";
            }

            // The parts of a version 7 look no longer exist: the owner takes the default look.
            if (version < 8)
            {
                data.appearance = new AppearanceSave();
            }

            if (version < 9)
            {
                data.ownerPronounsId ??= "";
            }

            // 9 -> 10: money was added; nothing could be earned before, so an older game has none.
            if (version < 10)
            {
                data.money = 0;
            }

            // 10 -> 11: stamina was added; an older game wakes up rested.
            if (version < 11)
            {
                data.stamina = -1;
            }

            // 11 -> 12: health was added; an older game wakes up healed.
            if (version < 12)
            {
                data.health = -1;
            }

            // 12 -> 13: the recap lists the day's sales; an older recap has none.
            if (version < 13)
            {
                data.recap ??= new DayRecap();
                data.recap.sales ??= new System.Collections.Generic.List<SaleEntry>();
            }

            // 13 -> 14: levels were added; an older game starts at level 1 and has made nothing yet.
            if (version < 14)
            {
                data.level = 1;
                data.xp = 0;
                data.skillPoints = 0;
                data.craftedItems ??= new System.Collections.Generic.List<string>();
            }

            data.version = SaveData.CurrentVersion;
            return data;
        }
    }
}
