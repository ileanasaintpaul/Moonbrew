using System.Collections.Generic;
using Moonbrew.Inventory;

namespace Moonbrew.Kitchen
{
    // The rules of the display case: a whole cake becomes its portions there, and portions leave one at a time.
    public static class DisplayCase
    {
        // Puts the cake of this owner slot in the case. All or nothing: a cake is never cut if a portion would be lost.
        public static bool Place(ItemInventory owner, int slot, ItemInventory display, IEnumerable<RecipeData> recipes)
        {
            if (owner == null || display == null || slot < 0 || slot >= owner.SlotCount || owner.Slots[slot].IsEmpty)
            {
                return false;
            }

            var recipe = CakeRecipe(owner.Slots[slot].Item, recipes);
            if (recipe == null || display.SpaceFor(recipe.Portion) < recipe.Portions)
            {
                return false;
            }

            owner.Remove(recipe.Result, 1);
            display.Add(recipe.Portion, recipe.Portions);
            return true;
        }

        // Moves a single portion to the owner. False when the slot is empty or the owner has no room.
        public static bool Take(ItemInventory display, int slot, ItemInventory owner)
        {
            return display != null && display.MoveSlot(slot, owner, 1) == 1;
        }

        private static RecipeData CakeRecipe(ItemData item, IEnumerable<RecipeData> recipes)
        {
            foreach (var recipe in recipes ?? new RecipeData[0])
            {
                if (recipe != null && recipe.Result == item && recipe.Portion != null)
                {
                    return recipe;
                }
            }

            return null;
        }
    }
}
