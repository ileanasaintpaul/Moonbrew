using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Kitchen
{
    // The player's recipe book in the scene: saved and restored with the game; the shop calls Learn.
    public class RecipeBookHolder : MonoBehaviour
    {
        [SerializeField] private RecipeCatalog catalog;
        [SerializeField] private EventBusAsset eventBus;

        private bool _subscribed;

        public RecipeBook Book { get; } = new RecipeBook();

        public RecipeCatalog Catalog => catalog;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(RecipeCatalog recipeCatalog, EventBusAsset bus)
        {
            catalog = recipeCatalog;
            eventBus = bus;
        }

        // Adds a recipe to the book and announces it. Returns false when it was already known.
        public bool Learn(RecipeData recipe)
        {
            if (!Book.Learn(recipe))
            {
                return false;
            }

            Debug.Log($"[Recipes] Learned {recipe.Id}");
            if (eventBus != null)
            {
                eventBus.Publish(new RecipeLearned(recipe.Id));
            }

            return true;
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<GameLoaded>(OnLoaded);
            eventBus.Subscribe<GameSaving>(OnSaving);
            eventBus.Subscribe<MenuRequested>(OnMenuRequested);
            eventBus.Subscribe<LevelReached>(OnLevelReached);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
            eventBus.Unsubscribe<GameSaving>(OnSaving);
            eventBus.Unsubscribe<MenuRequested>(OnMenuRequested);
            eventBus.Unsubscribe<LevelReached>(OnLevelReached);
            _subscribed = false;
        }

        private void OnLoaded(GameLoaded message)
        {
            Book.Restore(message.Data.learnedRecipes);
        }

        private void OnSaving(GameSaving message)
        {
            message.Data.learnedRecipes = Book.ToSave();
        }

        // A level's reward can teach a recipe.
        private void OnLevelReached(LevelReached message)
        {
            if (string.IsNullOrEmpty(message.RecipeId) || catalog == null)
            {
                return;
            }

            foreach (var recipe in catalog.Recipes)
            {
                if (recipe != null && recipe.Id == message.RecipeId)
                {
                    Learn(recipe);
                    return;
                }
            }
        }

        private void OnMenuRequested(MenuRequested message)
        {
            Book.FillMenu(catalog != null ? catalog.Recipes : null, message.Menu, message.MaxStars);
        }
    }
}
