using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Moonbrew.Core
{
    // One JSON file per game in a folder. Several games can exist; each keeps its own save.
    public class SaveStore
    {
        private const string FilePrefix = "game_";
        private const string FileExtension = ".json";

        private readonly string _folder;

        public SaveStore(string folder)
        {
            if (string.IsNullOrEmpty(folder))
            {
                throw new ArgumentException("The save folder is missing.", nameof(folder));
            }

            _folder = folder;
        }

        public static string NewSlotId()
        {
            return Guid.NewGuid().ToString("N");
        }

        // Written to a temporary file first, so a crash mid-write never leaves a half save.
        public void Save(SaveData data)
        {
            if (data == null || string.IsNullOrEmpty(data.slotId))
            {
                throw new ArgumentException("A save needs a slot ID.", nameof(data));
            }

            Directory.CreateDirectory(_folder);
            var path = PathFor(data.slotId);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, SaveSerializer.ToJson(data));
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temporary, path);
        }

        // Null when the game does not exist or its file cannot be read.
        public SaveData Load(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                return null;
            }

            var path = PathFor(slotId);
            return File.Exists(path) ? Read(path) : null;
        }

        // Every readable game, most recently played first.
        public List<SaveData> List()
        {
            var games = new List<SaveData>();
            if (!Directory.Exists(_folder))
            {
                return games;
            }

            foreach (var path in Directory.GetFiles(_folder, FilePrefix + "*" + FileExtension))
            {
                var data = Read(path);
                if (data != null)
                {
                    games.Add(data);
                }
            }

            games.Sort((a, b) => LastPlayed(b).CompareTo(LastPlayed(a)));
            return games;
        }

        public SaveData MostRecent()
        {
            var games = List();
            return games.Count > 0 ? games[0] : null;
        }

        public bool Delete(string slotId)
        {
            var path = string.IsNullOrEmpty(slotId) ? null : PathFor(slotId);
            if (path == null || !File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }

        private string PathFor(string slotId)
        {
            return Path.Combine(_folder, FilePrefix + slotId + FileExtension);
        }

        private static SaveData Read(string path)
        {
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Save] Cannot read {path}: {exception.Message}");
                return null;
            }

            if (SaveSerializer.TryFromJson(json, out var data, out var error))
            {
                return data;
            }

            Debug.LogWarning($"[Save] Ignoring {path}: {error}");
            return null;
        }

        private static DateTime LastPlayed(SaveData data)
        {
            return DateTime.TryParse(data.lastPlayedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                ? date
                : DateTime.MinValue;
        }
    }
}
