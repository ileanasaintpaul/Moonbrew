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

            data.version = SaveData.CurrentVersion;
            return data;
        }
    }
}
