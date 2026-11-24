using System.Collections.Generic;
using Moonbrew.Npc;

namespace Moonbrew.Ui
{
    // Who speaks when a table orders, and their words with the owner's name and the dish filled in.
    public static class DialogueScript
    {
        public struct Turn
        {
            public VillagerData Speaker;
            public string DishId;
        }

        // Everyone at the table who has something to say, in their seat order, each with the dish they chose.
        public static List<Turn> Speakers(IReadOnlyList<string> customerIds, IReadOnlyList<string> dishIds, VillagerCatalog voices)
        {
            var turns = new List<Turn>();
            if (customerIds == null || voices == null)
            {
                return turns;
            }

            for (var i = 0; i < customerIds.Count; i++)
            {
                var voice = voices.Find(customerIds[i]);
                if (voice != null && SpokenLines(voice) > 0)
                {
                    turns.Add(new Turn { Speaker = voice, DishId = dishIds != null && i < dishIds.Count ? dishIds[i] : null });
                }
            }

            return turns;
        }

        // The owner never answers when someone orders: only the customer's own lines are said.
        public static int SpokenLines(VillagerData voice)
        {
            var count = 0;
            foreach (var line in voice.OrderLines)
            {
                if (line.Speaker == Speaker.Villager)
                {
                    count++;
                }
            }

            return count;
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
