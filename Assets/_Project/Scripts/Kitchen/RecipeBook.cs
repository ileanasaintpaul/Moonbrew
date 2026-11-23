using System.Collections.Generic;
using Moonbrew.Core;

namespace Moonbrew.Kitchen
{
    // Which recipes the player knows: the ones known from the start, plus the ones learned (bought).
    public class RecipeBook
    {
        private readonly HashSet<string> _learned = new HashSet<string>();

        public bool IsKnown(RecipeData recipe)
        {
            return recipe != null && (recipe.KnownAtStart || _learned.Contains(recipe.Id));
        }

        // Returns false when the recipe was already known.
        public bool Learn(RecipeData recipe)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.Id) || IsKnown(recipe))
            {
                return false;
            }

            _learned.Add(recipe.Id);
            return true;
        }

        // Adds the dish of every known recipe to the menu, once each.
        public void FillMenu(IEnumerable<RecipeData> recipes, List<MenuDish> menu)
        {
            if (recipes == null || menu == null)
            {
                return;
            }

            foreach (var recipe in recipes)
            {
                if (!IsKnown(recipe) || recipe.Result == null || menu.Exists(dish => dish.DishId == recipe.Result.Id))
                {
                    continue;
                }

                var ingredients = new List<string>();
                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient.Item != null)
                    {
                        ingredients.Add(ingredient.Item.Id);
                    }
                }

                menu.Add(new MenuDish(recipe.Result.Id, ingredients));
            }
        }

        public List<string> ToSave()
        {
            return new List<string>(_learned);
        }

        public void Restore(IEnumerable<string> learned)
        {
            _learned.Clear();
            foreach (var id in learned ?? new List<string>())
            {
                if (!string.IsNullOrEmpty(id))
                {
                    _learned.Add(id);
                }
            }
        }
    }
}
