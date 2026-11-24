using System.Collections.Generic;
using Moonbrew.Npc;

namespace Moonbrew.Ui
{
    // Who orders what at a table, and the conversation a villager has about it, with names and dish filled in.
    public static class DialogueScript
    {
        // The first villager of the table who has something to say, and the dish they ordered.
        public static bool FindSpeaker(IReadOnlyList<string> customerIds, IReadOnlyList<string> dishIds, VillagerCatalog villagers,
            out VillagerData villager, out string dishId)
        {
            villager = null;
            dishId = null;
            if (customerIds == null || villagers == null)
            {
                return false;
            }

            for (var i = 0; i < customerIds.Count; i++)
            {
                var found = villagers.Find(customerIds[i]);
                if (found != null && found.OrderLines.Count > 0)
                {
                    villager = found;
                    dishId = dishIds != null && i < dishIds.Count ? dishIds[i] : null;
                    return true;
                }
            }

            return false;
        }

        public static string Fill(string text, string ownerName, string dishName)
        {
            return (text ?? string.Empty).Replace("{owner}", ownerName ?? string.Empty).Replace("{dish}", dishName ?? string.Empty);
        }

        // How many letters show after this many seconds of typing.
        public static int Revealed(string text, float seconds, float lettersPerSecond)
        {
            var length = text?.Length ?? 0;
            if (lettersPerSecond <= 0f)
            {
                return length;
            }

            if (seconds <= 0f)
            {
                return 0;
            }

            return seconds >= length / lettersPerSecond ? length : (int)(seconds * lettersPerSecond);
        }
    }
}
