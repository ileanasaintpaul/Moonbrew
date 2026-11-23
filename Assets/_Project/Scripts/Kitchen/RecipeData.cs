using System;
using System.Collections.Generic;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Kitchen
{
    // A dish or drink the café can make: its ingredients, what it gives and how long it cooks.
    [CreateAssetMenu(menuName = "Moonbrew/Kitchen/Recipe", fileName = "SO_Recipe_")]
    public class RecipeData : ScriptableObject
    {
        public const int MaxStars = 5;

        [Tooltip("Stable snake_case ID stored in saves, e.g. recipe_wheat_cake. Never change it once shipped.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Ingredient[] ingredients = new Ingredient[0];
        [SerializeField] private ItemData result;
        [SerializeField, Min(1)] private int resultQuantity = 1;
        [Tooltip("What one serving of a cake is, once it is in the display case. Empty for a single-serving dish, like a drink.")]
        [SerializeField] private ItemData portion;
        [Tooltip("Servings a cake gives in the display case.")]
        [SerializeField, Min(1)] private int portions = 6;
        [SerializeField, Range(1, MaxStars)] private int stars = 1;
        [Tooltip("Game minutes in the oven.")]
        [SerializeField, Min(0)] private int minutes = 30;
        [Tooltip("Known from the start of a game; otherwise it has to be bought first.")]
        [SerializeField] private bool knownAtStart = true;
        [Tooltip("Price of the recipe in the shop, when it is not known at start.")]
        [SerializeField, Min(0)] private int price;

        public string Id => id;

        public string DisplayName => displayName;

        public IReadOnlyList<Ingredient> Ingredients => ingredients;

        public ItemData Result => result;

        public int ResultQuantity => resultQuantity;

        public ItemData Portion => portion;

        public int Portions => portions;

        // What a customer orders: a slice of a cake, or the dish itself.
        public ItemData ServedItem => portion != null ? portion : result;

        public int Stars => stars;

        public int Minutes => minutes;

        public bool KnownAtStart => knownAtStart;

        public int Price => price;

        public Job ToJob()
        {
            return new Job(id, result, resultQuantity, minutes);
        }

        // True when every ingredient is there in the needed quantity.
        public bool HasIngredients(Func<ItemData, int> count)
        {
            if (count == null)
            {
                return false;
            }

            foreach (var ingredient in ingredients)
            {
                if (ingredient.Item == null || count(ingredient.Item) < ingredient.Quantity)
                {
                    return false;
                }
            }

            return true;
        }

        // For recipes built from code.
        public void Init(string recipeId, Ingredient[] recipeIngredients, ItemData resultItem, int recipeStars, int cookMinutes, bool known)
        {
            id = recipeId;
            ingredients = recipeIngredients ?? new Ingredient[0];
            result = resultItem;
            stars = recipeStars;
            minutes = cookMinutes;
            knownAtStart = known;
        }

        // For cakes built from code.
        public void InitPortions(ItemData portionItem, int portionCount)
        {
            portion = portionItem;
            portions = portionCount;
        }
    }

    [Serializable]
    public struct Ingredient
    {
        [SerializeField] private ItemData item;
        [SerializeField, Min(1)] private int quantity;

        public Ingredient(ItemData item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }

        public ItemData Item => item;

        public int Quantity => quantity;
    }
}
