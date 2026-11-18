using System.Collections.Generic;

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
