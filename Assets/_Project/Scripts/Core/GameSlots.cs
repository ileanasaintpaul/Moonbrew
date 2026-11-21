using System;
using System.Collections.Generic;
using System.Globalization;

namespace Moonbrew.Core
{
    // The saved games as the main menu sees them. The session always opens the game played last,
    // so starting a game only has to make it the most recent one.
    public class GameSlots
    {
        private readonly SaveStore _store;

        public GameSlots(SaveStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool CanContinue => _store.MostRecent() != null;

        // Most recently played first.
        public List<SaveData> List()
        {
            return _store.List();
        }

        // False when the game no longer exists.
        public bool Choose(string slotId, DateTime nowUtc)
        {
            var data = _store.Load(slotId);
            if (data == null)
            {
                return false;
            }

            data.lastPlayedUtc = Stamp(nowUtc);
            _store.Save(data);
            return true;
        }

        // The name is trimmed; no look keeps the default one.
        public SaveData CreateNew(int firstDay, DateTime nowUtc, string ownerName = "", AppearanceSave appearance = null)
        {
            var data = SaveData.CreateNew(SaveStore.NewSlotId(), firstDay);
            data.lastPlayedUtc = Stamp(nowUtc);
            data.ownerName = ownerName?.Trim() ?? "";
            data.appearance = appearance ?? new AppearanceSave();
            _store.Save(data);
            return data;
        }

        private static string Stamp(DateTime nowUtc)
        {
            return nowUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        }
    }
}
