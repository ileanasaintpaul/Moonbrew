using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Kitchen;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // The game menu with tabs, like Stardew's; only the Recipes tab for now. Opened with its key anywhere
    // (to read the recipes), or by a cooking station, which enables the Cook buttons. Display only:
    // it reads the recipe book and the bag, and forwards the chosen recipe (RecipeChosen).
    [RequireComponent(typeof(UIDocument))]
    public class GameMenu : MonoBehaviour
    {
        private const string RootName = "game-menu";
        private const string RowName = "recipe";
        private const string LockedClass = "recipe--locked";

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private RecipeBookHolder recipeBook;
        [SerializeField] private InventoryHolder bag;
        [SerializeField] private VisualTreeAsset menuTemplate;
        [SerializeField] private VisualTreeAsset rowTemplate;
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private InputActionReference toggleAction;
        [SerializeField] private string star = "★";
        [SerializeField] private string titleFormat = "{0}  {1}";
        [SerializeField] private string ingredientFormat = "{0} {1}/{2}";
        [SerializeField] private string toBuyText = "À acheter";
        [SerializeField] private string missingText = "Il manque des ingrédients";
        [SerializeField] private string atStoveHint = "Choisis une recette à cuisiner.";
        [SerializeField] private string elsewhereHint = "Va au four du café pour cuisiner.";

        private readonly List<VisualElement> _rows = new List<VisualElement>();
        private UIDocument _document;
        private VisualElement _menu;
        private string _stoveId;
        private bool _subscribed;

        public bool IsOpen => _menu != null;

        public int RowCount => _rows.Count;

        public bool CanCook(int index) => _rows[index].Q<Button>("cook").enabledSelf;

        public string StatusText(int index) => _rows[index].Q<Label>("status").text;

        public void Init(EventBusAsset bus, RecipeBookHolder book, InventoryHolder playerBag, VisualTreeAsset menu, VisualTreeAsset row)
        {
            Unsubscribe();
            eventBus = bus;
            recipeBook = book;
            bag = playerBag;
            menuTemplate = menu;
            rowTemplate = row;
            Subscribe();
        }

        // stoveId: the cooking station that opened the menu, or null when opened from the keyboard.
        public void Open(string stoveId)
        {
            Close();
            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null || menuTemplate == null)
            {
                return;
            }

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            _stoveId = stoveId;
            _menu = menuTemplate.Instantiate().Q(RootName);
            _menu.Q<Button>("close").clicked += Close;
            _menu.Q<Label>("hint").text = stoveId != null ? atStoveHint : elsewhereHint;
            root.Add(_menu);
            BuildRecipes();
            eventBus.Publish(new GameMenuToggled(true));
        }

        public void Close()
        {
            if (_menu == null)
            {
                return;
            }

            _menu.RemoveFromHierarchy();
            _menu = null;
            _rows.Clear();
            _stoveId = null;
            eventBus.Publish(new GameMenuToggled(false));
        }

        // The Cook button of a row; also used by tests.
        public void ClickCook(int index)
        {
            var recipe = recipeBook.Catalog.Recipes[index];
            var stoveId = _stoveId;
            if (stoveId == null || !CanCook(index))
            {
                return;
            }

            Close();
            eventBus.Publish(new RecipeChosen(stoveId, recipe.Id));
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            Subscribe();
            if (toggleAction != null)
            {
                toggleAction.action.performed += OnToggle;
                toggleAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (toggleAction != null)
            {
                toggleAction.action.performed -= OnToggle;
            }

            Close();
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open(null);
            }
        }

        private void OnStoveOpened(StoveOpened message)
        {
            Open(message.StationId);
        }

        private void BuildRecipes()
        {
            var content = _menu.Q<ScrollView>("content");
            var recipes = recipeBook != null && recipeBook.Catalog != null ? recipeBook.Catalog.Recipes : new RecipeData[0];
            for (var i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                var row = rowTemplate.Instantiate().Q(RowName);
                var known = recipeBook.Book.IsKnown(recipe);
                var hasIngredients = recipe.HasIngredients(bag.Inventory.Count);

                row.Q("icon").style.backgroundImage = recipe.Result != null && recipe.Result.Icon != null
                    ? new StyleBackground(recipe.Result.Icon)
                    : new StyleBackground(StyleKeyword.None);
                row.Q<Label>("title").text = string.Format(titleFormat, recipe.DisplayName, RecipeText.Stars(recipe.Stars, star));
                row.Q<Label>("ingredients").text = RecipeText.Ingredients(recipe, bag.Inventory.Count, ingredientFormat);
                var status = row.Q<Label>("status");
                status.text = !known ? toBuyText : !hasIngredients ? missingText : string.Empty;
                status.style.display = status.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                row.EnableInClassList(LockedClass, !known);

                var cook = row.Q<Button>("cook");
                cook.SetEnabled(_stoveId != null && known && hasIngredients);
                var index = i;
                cook.clicked += () => ClickCook(index);

                content.Add(row);
                _rows.Add(row);
            }
        }

        private void Subscribe()
        {
            if (_subscribed || eventBus == null || !isActiveAndEnabled)
            {
                return;
            }

            eventBus.Subscribe<StoveOpened>(OnStoveOpened);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && eventBus != null)
            {
                eventBus.Unsubscribe<StoveOpened>(OnStoveOpened);
            }

            _subscribed = false;
        }
    }
}
