using System.Collections.Generic;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Cafe
{
    // A café customer. A villager's ID is also the one the village NPCs use, so both share a relationship.
    [CreateAssetMenu(menuName = "Moonbrew/Cafe/Customer", fileName = "SO_Customer_")]
    public class CustomerData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private CustomerKind kind;
        [SerializeField] private GameObject model;

        // Dishes or ingredients: a dish is loved or disliked for itself, or for what it is made of.
        [SerializeField] private ItemData[] loved = new ItemData[0];
        [SerializeField] private ItemData[] disliked = new ItemData[0];

        // What they say at the café; a villager's lines hint at their tastes, nothing is written down for the player.
        [SerializeField, TextArea] private string[] lines = new string[0];

        public string Id => id;

        public string DisplayName => displayName;

        public CustomerKind Kind => kind;

        public GameObject Model => model;

        public IReadOnlyList<ItemData> Loved => loved;

        public IReadOnlyList<ItemData> Disliked => disliked;

        public IReadOnlyList<string> Lines => lines;

        // What the customer thinks of a dish. A taste for the dish itself wins; otherwise one disliked
        // ingredient is enough to spoil it, and one loved ingredient is enough to please.
        public Taste TasteOf(ItemData dish, IEnumerable<ItemData> ingredients)
        {
            if (dish == null)
            {
                return Taste.Neutral;
            }

            if (Contains(disliked, dish))
            {
                return Taste.Disliked;
            }

            if (Contains(loved, dish))
            {
                return Taste.Loved;
            }

            var taste = Taste.Neutral;
            foreach (var ingredient in ingredients ?? new ItemData[0])
            {
                if (Contains(disliked, ingredient))
                {
                    return Taste.Disliked;
                }

                if (Contains(loved, ingredient))
                {
                    taste = Taste.Loved;
                }
            }

            return taste;
        }

        // For customers built from code.
        public void Init(string customerId, CustomerKind customerKind, ItemData[] lovedItems, ItemData[] dislikedItems, string[] customerLines = null)
        {
            id = customerId;
            kind = customerKind;
            loved = lovedItems ?? new ItemData[0];
            disliked = dislikedItems ?? new ItemData[0];
            lines = customerLines ?? new string[0];
        }

        private static bool Contains(ItemData[] items, ItemData item)
        {
            return item != null && System.Array.IndexOf(items, item) >= 0;
        }
    }
}
