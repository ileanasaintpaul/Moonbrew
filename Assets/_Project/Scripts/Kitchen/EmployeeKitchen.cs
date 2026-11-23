using System;
using System.Collections.Generic;
using Moonbrew.Inventory;

namespace Moonbrew.Kitchen
{
    // How the employee gets a whole order: what is ready first (display case, then fridge), else cooked from the fridge.
    public static class EmployeeKitchen
    {
        // All or nothing: works on copies, so a missing dish leaves both inventories untouched.
        public static bool Prepare(IReadOnlyList<string> dishIds, int maxStars, ItemInventory fridge, ItemInventory display,
            IEnumerable<RecipeData> recipes, Func<string, ItemData> findItem)
        {
            if (dishIds == null || dishIds.Count == 0 || fridge == null || display == null || recipes == null || findItem == null)
            {
                return false;
            }

            var fridgeCopy = Copy(fridge, findItem);
            var displayCopy = Copy(display, findItem);
            foreach (var dishId in dishIds)
            {
                var recipe = RecipeFor(dishId, recipes);
                if (recipe == null || recipe.Stars > maxStars)
                {
                    return false;
                }

                var dish = recipe.ServedItem;
                if (!displayCopy.Remove(dish, 1) && !fridgeCopy.Remove(dish, 1) && !Cook(recipe, fridgeCopy, displayCopy))
                {
                    return false;
                }
            }

            fridge.Restore(fridgeCopy.ToSave(string.Empty), findItem);
            display.Restore(displayCopy.ToSave(string.Empty), findItem);
            return true;
        }

        // Serves one and stores the rest: a cake's other portions in the display case, extra results in the fridge.
        private static bool Cook(RecipeData recipe, ItemInventory fridge, ItemInventory display)
        {
            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null || !fridge.Remove(ingredient.Item, ingredient.Quantity))
                {
                    return false;
                }
            }

            if (recipe.Portion != null)
            {
                return Store(display, recipe.Portion, recipe.ResultQuantity * recipe.Portions - 1);
            }

            return Store(fridge, recipe.Result, recipe.ResultQuantity - 1);
        }

        private static bool Store(ItemInventory inventory, ItemData item, int quantity)
        {
            return quantity <= 0 || inventory.Add(item, quantity) == quantity;
        }

        private static RecipeData RecipeFor(string dishId, IEnumerable<RecipeData> recipes)
        {
            foreach (var recipe in recipes)
            {
                if (recipe != null && recipe.ServedItem != null && recipe.ServedItem.Id == dishId)
                {
                    return recipe;
                }
            }

            return null;
        }

        private static ItemInventory Copy(ItemInventory inventory, Func<string, ItemData> findItem)
        {
            var copy = new ItemInventory(inventory.SlotCount);
            copy.Restore(inventory.ToSave(string.Empty), findItem);
            return copy;
        }
    }
}
