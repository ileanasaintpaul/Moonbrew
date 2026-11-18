using System;
using System.Text;
using Moonbrew.Inventory;
using Moonbrew.Kitchen;

namespace Moonbrew.Ui
{
    public static class RecipeText
    {
        public static string Stars(int stars, string star)
        {
            var text = new StringBuilder();
            for (var i = 0; i < Math.Max(0, stars); i++)
            {
                text.Append(star);
            }

            return text.ToString();
        }

        // "Farine 1/2, Fraise 3/3": what the bag holds out of what the recipe needs.
        public static string Ingredients(RecipeData recipe, Func<ItemData, int> count, string ingredientFormat)
        {
            if (recipe == null || count == null || ingredientFormat == null)
            {
                throw new ArgumentNullException(recipe == null ? nameof(recipe) : count == null ? nameof(count) : nameof(ingredientFormat));
            }

            var text = new StringBuilder();
            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null)
                {
                    continue;
                }

                if (text.Length > 0)
                {
                    text.Append(", ");
                }

                text.AppendFormat(ingredientFormat, ingredient.Item.DisplayName, Math.Min(count(ingredient.Item), ingredient.Quantity), ingredient.Quantity);
            }

            return text.ToString();
        }
    }
}
