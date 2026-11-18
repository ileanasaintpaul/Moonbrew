using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Kitchen
{
    // Every recipe of the game, in the order the menu lists them.
    [CreateAssetMenu(menuName = "Moonbrew/Kitchen/Recipe Catalog", fileName = "SO_RecipeCatalog")]
    public class RecipeCatalog : ScriptableObject
    {
        [SerializeField] private RecipeData[] recipes = new RecipeData[0];

        public IReadOnlyList<RecipeData> Recipes => recipes;

        // For catalogs built from code.
        public void Init(RecipeData[] catalogRecipes)
        {
            recipes = catalogRecipes ?? new RecipeData[0];
        }

        public RecipeData Find(string id)
        {
            foreach (var recipe in recipes)
            {
                if (recipe != null && !string.IsNullOrEmpty(id) && recipe.Id == id)
                {
                    return recipe;
                }
            }

            return null;
        }
    }
}
