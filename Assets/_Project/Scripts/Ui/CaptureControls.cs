using Moonbrew.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // Debug shortcuts: hide the HUD and control the clock to film the devlog, start a new game. Debug builds only.
    public class CaptureControls : MonoBehaviour
    {
        [SerializeField] private CaptureConfig config;
        [SerializeField] private GameClockRunner clockRunner;
        [SerializeField] private UIDocument[] hudDocuments;
        [SerializeField] private InputActionReference toggleHudAction;
        [SerializeField] private InputActionReference pauseClockAction;
        [SerializeField] private InputActionReference advanceHourAction;
        [SerializeField] private InputActionReference nextPhaseAction;
        [SerializeField] private GameSession session;
        [SerializeField] private InputActionReference newGameAction;
        [SerializeField] private Moonbrew.Kitchen.RecipeBookHolder recipeBook;
        [SerializeField] private InputActionReference learnRecipesAction;
        [SerializeField] private InputActionReference stockIngredientsAction;
        [SerializeField] private InputActionReference jumpToOpeningAction;
        [Tooltip("The café fridge the \"stock\" shortcut fills.")]
        [SerializeField] private Moonbrew.Inventory.InventoryHolder fridge;

        public bool HudVisible { get; private set; } = true;

        public void Init(CaptureConfig captureConfig, GameClockRunner runner, UIDocument[] documents)
        {
            config = captureConfig;
            clockRunner = runner;
            hudDocuments = documents;
        }

        public void SetHudVisible(bool visible)
        {
            HudVisible = visible;
            foreach (var document in hudDocuments)
            {
                if (document != null && document.rootVisualElement != null)
                {
                    document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Subscribe(toggleHudAction, OnToggleHud);
            Subscribe(pauseClockAction, OnPauseClock);
            Subscribe(advanceHourAction, OnAdvanceHour);
            Subscribe(nextPhaseAction, OnNextPhase);
            Subscribe(newGameAction, OnNewGame);
            Subscribe(learnRecipesAction, OnLearnRecipes);
            Subscribe(stockIngredientsAction, OnStockIngredients);
            Subscribe(jumpToOpeningAction, OnJumpToOpening);
        }

        private void OnDisable()
        {
            Unsubscribe(toggleHudAction, OnToggleHud);
            Unsubscribe(pauseClockAction, OnPauseClock);
            Unsubscribe(advanceHourAction, OnAdvanceHour);
            Unsubscribe(nextPhaseAction, OnNextPhase);
            Unsubscribe(newGameAction, OnNewGame);
            Unsubscribe(learnRecipesAction, OnLearnRecipes);
            Unsubscribe(stockIngredientsAction, OnStockIngredients);
            Unsubscribe(jumpToOpeningAction, OnJumpToOpening);
            SetHudVisible(true);
        }

        private void OnToggleHud(InputAction.CallbackContext context)
        {
            SetHudVisible(!HudVisible);
        }

        private void OnPauseClock(InputAction.CallbackContext context)
        {
            clockRunner.Clock.SetPaused(!clockRunner.Clock.IsPaused);
            Debug.Log($"[Capture] Clock {(clockRunner.Clock.IsPaused ? "paused" : "running")}");
        }

        private void OnAdvanceHour(InputAction.CallbackContext context)
        {
            clockRunner.Clock.AdvanceMinutes(config.AdvanceMinutes);
        }

        private void OnNextPhase(InputAction.CallbackContext context)
        {
            clockRunner.Clock.SkipToNextPhase();
        }

        // Until the shop sells them, learns every recipe of the game to test them.
        private void OnLearnRecipes(InputAction.CallbackContext context)
        {
            if (recipeBook == null || recipeBook.Catalog == null)
            {
                return;
            }

            foreach (var recipe in recipeBook.Catalog.Recipes)
            {
                recipeBook.Learn(recipe);
            }
        }

        // To test every dish: the ingredients of each recipe, several times over, straight into the café fridge.
        private void OnStockIngredients(InputAction.CallbackContext context)
        {
            if (recipeBook == null || recipeBook.Catalog == null || fridge == null)
            {
                return;
            }

            foreach (var recipe in recipeBook.Catalog.Recipes)
            {
                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient.Item != null)
                    {
                        fridge.Inventory.Add(ingredient.Item, ingredient.Quantity * config.StockBatches);
                    }
                }
            }
        }

        // Straight to the café's opening hour, when it is still ahead.
        private void OnJumpToOpening(InputAction.CallbackContext context)
        {
            var minutes = config.JumpMinute - clockRunner.Clock.Minutes;
            if (minutes > 0)
            {
                clockRunner.Clock.AdvanceMinutes(minutes);
            }
        }

        // Starts a fresh game in its own save file; the other games keep theirs.
        private void OnNewGame(InputAction.CallbackContext context)
        {
            if (session != null)
            {
                session.StartNewGame();
            }
        }

        private static void Subscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            if (reference != null)
            {
                reference.action.performed += handler;
                reference.action.Enable();
            }
        }

        private static void Unsubscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            if (reference != null)
            {
                reference.action.performed -= handler;
            }
        }
    }
}
