using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // The character creation screen: a tab per part with each choice drawn on the owner and, under it, the colour that
    // goes with it (skin with the body, hair colour with the cut, eyebrows with the beard); then the outfit colours and the name.
    // The look in progress lives in CharacterDraft; this screen shows it on the preview model and passes Start and Back on.
    [RequireComponent(typeof(UIDocument))]
    public class CharacterCreation : MonoBehaviour
    {
        public enum Page
        {
            Body,
            Hair,
            Beard,
            Hat,
            Accessory,
            Outfit,
            Name,
        }

        private const string ShownClass = "panel-page--shown";
        private const string SelectedTabClass = "tab--selected";
        private const string CellClass = "cell";
        private const string SelectedCellClass = "cell--selected";
        private const string SwatchClass = "swatch";
        private const string SelectedSwatchClass = "swatch--selected";
        private const string PartColoursRow = "part-colours";

        private static readonly string[] TabNames = { "tab-body", "tab-hair", "tab-beard", "tab-hat", "tab-accessory", "tab-outfit", "tab-name" };
        private static readonly string[] Titles = { "Body", "Hair", "Beard", "Hat", "Accessory", "Outfit", "Name" };
        private static readonly AppearanceColourSlot[] OutfitSlots = { AppearanceColourSlot.Top, AppearanceColourSlot.Bottom };
        private static readonly string[] OutfitRows = { "top-colours", "bottom-colours" };

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private AppearanceCatalog catalog;
        [Tooltip("The owner the player sees, dressed as the look changes.")]
        [SerializeField] private OwnerAppearance preview;
        [SerializeField] private AppearanceThumbnails thumbnails;
        [SerializeField] private string noneText = "None";
        [SerializeField] private string counterFormat = "{0} / {1}";
        [Tooltip("Above the colours of the Body, Hair and Beard pages.")]
        [SerializeField] private string skinTitle = "Skin";
        [SerializeField] private string hairColourTitle = "Colour";
        [SerializeField] private string browTitle = "Eyebrows";
        [SerializeField] private InputAction previousTab = new InputAction("Previous Tab", InputActionType.Button, "<Keyboard>/q");
        [SerializeField] private InputAction nextTab = new InputAction("Next Tab", InputActionType.Button, "<Keyboard>/e");

        private readonly List<Button> _cells = new List<Button>();
        private readonly List<string> _cellIds = new List<string>();
        private VisualElement _root;
        private TextField _nameField;
        private CharacterDraft _draft;
        private System.Random _random;

        public Page CurrentPage { get; private set; }

        public CharacterDraft Draft => _draft;

        public int CellCount => _cells.Count;

        public Button CellAt(int index) => _cells[index];

        public Button ButtonNamed(string buttonName) => _root?.Q<Button>(buttonName);

        public TextField NameField => _nameField;

        // The swatches of a colour shown on the current page.
        public Button SwatchAt(AppearanceColourSlot slot, int index) => RowOf(slot).Query<Button>().AtIndex(index);

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(EventBusAsset bus, AppearanceCatalog looks, OwnerAppearance previewModel, AppearanceThumbnails thumbnailRenderer, System.Random random)
        {
            eventBus = bus;
            catalog = looks;
            preview = previewModel;
            thumbnails = thumbnailRenderer;
            _random = random;
        }

        public void ShowPage(Page page)
        {
            CurrentPage = page;
            for (var i = 0; i < TabNames.Length; i++)
            {
                ButtonNamed(TabNames[i]).EnableInClassList(SelectedTabClass, i == (int)page);
            }

            _root.Q<Label>("panel-title").text = Titles[(int)page];
            var category = CategoryOf(page);
            _root.Q("parts").EnableInClassList(ShownClass, category.HasValue);
            _root.Q("outfit").EnableInClassList(ShownClass, page == Page.Outfit);
            _root.Q("finish").EnableInClassList(ShownClass, page == Page.Name);
            Rebuild();
        }

        private void Awake()
        {
            if (eventBus == null || catalog == null || preview == null || thumbnails == null)
            {
                Debug.LogError($"{nameof(CharacterCreation)} on {name} needs the event bus, the catalog, the preview model and the thumbnails.", this);
                enabled = false;
                return;
            }

            _random ??= new System.Random();
            _draft = new CharacterDraft(catalog);
        }

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            for (var i = 0; i < TabNames.Length; i++)
            {
                var page = (Page)i;
                ButtonNamed(TabNames[i]).clicked += () => ShowPage(page);
            }

            ButtonNamed("random").clicked += OnRandom;
            ButtonNamed("start").clicked += OnStart;
            ButtonNamed("back").clicked += OnBack;
            _nameField = _root.Q<TextField>("owner-name");
            _nameField.maxLength = catalog.MaxNameLength;
            _nameField.RegisterValueChangedCallback(OnNameChanged);
            previousTab.performed += OnPreviousTab;
            nextTab.performed += OnNextTab;
            previousTab.Enable();
            nextTab.Enable();
        }

        private void OnDisable()
        {
            previousTab.performed -= OnPreviousTab;
            nextTab.performed -= OnNextTab;
            previousTab.Disable();
            nextTab.Disable();
        }

        // After every Awake: the models are ready to be dressed and drawn.
        private void Start()
        {
            preview.Apply(_draft.Look);
            ButtonNamed("start").SetEnabled(_draft.CanConfirm);
            ShowPage(Page.Body);
        }

        private static AppearanceCategory? CategoryOf(Page page)
        {
            return page <= Page.Accessory ? (AppearanceCategory)page : (AppearanceCategory?)null;
        }

        // The colour chosen on a part's page, if it has one.
        private static AppearanceColourSlot? ColourOf(AppearanceCategory category)
        {
            switch (category)
            {
                case AppearanceCategory.Body: return AppearanceColourSlot.Skin;
                case AppearanceCategory.Hair: return AppearanceColourSlot.Hair;
                case AppearanceCategory.Beard: return AppearanceColourSlot.Brows;
                default: return null;
            }
        }

        private VisualElement RowOf(AppearanceColourSlot slot)
        {
            var outfit = System.Array.IndexOf(OutfitSlots, slot);
            return _root.Q(outfit >= 0 ? OutfitRows[outfit] : PartColoursRow);
        }

        // Redraws the current page: the thumbnails show the look as it is now.
        private void Rebuild()
        {
            var category = CategoryOf(CurrentPage);
            if (category.HasValue)
            {
                BuildCells(category.Value);
                BuildPartColours(category.Value);
            }
            else if (CurrentPage == Page.Outfit)
            {
                BuildOutfitSwatches();
            }

            preview.Apply(_draft.Look);
            ShowSelection();
        }

        // A cell per part of the category, plus None for the optional ones; each shows the current look wearing that part.
        private void BuildCells(AppearanceCategory category)
        {
            var grid = _root.Q("grid");
            grid.Clear();
            _cells.Clear();
            _cellIds.Clear();
            var framing = category == AppearanceCategory.Body ? AppearanceThumbnails.Framing.Body : AppearanceThumbnails.Framing.Head;
            if (AppearanceCatalog.IsOptional(category))
            {
                AddCell(grid, "", noneText, null);
            }

            foreach (var part in catalog.PartsOf(category))
            {
                var look = LookWith(category, part.Id);
                AddCell(grid, part.Id, null, thumbnails.Draw(look, framing, _cells.Count));
            }

            MarkCells(category);
        }

        private void AddCell(VisualElement grid, string id, string text, RenderTexture thumbnail)
        {
            var cell = new Button { text = text ?? "" };
            cell.AddToClassList(CellClass);
            if (thumbnail != null)
            {
                cell.style.backgroundImage = Background.FromRenderTexture(thumbnail);
            }

            cell.clicked += () => OnCellClicked(id);
            grid.Add(cell);
            _cells.Add(cell);
            _cellIds.Add(id);
        }

        private void MarkCells(AppearanceCategory category)
        {
            var chosen = _draft.ChosenId(category);
            for (var i = 0; i < _cells.Count; i++)
            {
                _cells[i].EnableInClassList(SelectedCellClass, _cellIds[i] == chosen);
            }

            var index = _cellIds.IndexOf(chosen);
            _root.Q<Label>("counter").text = string.Format(counterFormat, index + 1, _cells.Count);
        }

        private AppearanceSave LookWith(AppearanceCategory category, string id)
        {
            var look = _draft.Look;
            switch (category)
            {
                case AppearanceCategory.Body: look.bodyId = id; break;
                case AppearanceCategory.Hair: look.hairId = id; break;
                case AppearanceCategory.Beard: look.beardId = id; break;
                case AppearanceCategory.Hat: look.hatId = id; break;
                default: look.accessoryId = id; break;
            }

            return look;
        }

        private void OnCellClicked(string id)
        {
            var category = CategoryOf(CurrentPage);
            if (category.HasValue && _draft.Choose(category.Value, id))
            {
                preview.Apply(_draft.Look);
                MarkCells(category.Value);
                ShowSelection();
            }
        }

        // Under the cells: plain swatches of the colour that goes with the part, or nothing.
        private void BuildPartColours(AppearanceCategory category)
        {
            var slot = ColourOf(category);
            _root.Q("part-colour-section").EnableInClassList(ShownClass, slot.HasValue);
            var row = _root.Q(PartColoursRow);
            row.Clear();
            if (!slot.HasValue)
            {
                return;
            }

            _root.Q<Label>("part-colour-title").text = slot == AppearanceColourSlot.Skin ? skinTitle : slot == AppearanceColourSlot.Hair ? hairColourTitle : browTitle;
            foreach (var colour in catalog.ColoursOf(slot.Value))
            {
                Swatch(row, slot.Value, colour).style.backgroundColor = colour.Swatch;
            }
        }

        // Top and bottom colours drawn on the owner, framed on the top or the bottom alone, since a palette changes several colours.
        private void BuildOutfitSwatches()
        {
            var thumbnail = 0;
            for (var i = 0; i < OutfitSlots.Length; i++)
            {
                var slot = OutfitSlots[i];
                var framing = slot == AppearanceColourSlot.Top ? AppearanceThumbnails.Framing.Top : AppearanceThumbnails.Framing.Bottom;
                var row = _root.Q(OutfitRows[i]);
                row.Clear();
                var look = _draft.Look;
                foreach (var colour in catalog.ColoursOf(slot))
                {
                    AppearanceCatalog.SetColourId(look, slot, colour.Id);
                    Swatch(row, slot, colour).style.backgroundImage = Background.FromRenderTexture(thumbnails.Draw(look, framing, thumbnail++));
                }
            }
        }

        private Button Swatch(VisualElement row, AppearanceColourSlot slot, AppearanceColour colour)
        {
            var swatch = new Button();
            swatch.AddToClassList(SwatchClass);
            swatch.EnableInClassList(SelectedSwatchClass, colour.Id == _draft.ColourId(slot));
            swatch.clicked += () => OnColour(slot, colour.Id);
            row.Add(swatch);
            return swatch;
        }

        private void OnColour(AppearanceColourSlot slot, string id)
        {
            if (_draft.ChooseColour(slot, id))
            {
                Rebuild();
            }
        }

        // Under the owner: the chosen part of the page and its colour, the outfit colours, or the name.
        private void ShowSelection()
        {
            var label = _root.Q<Label>("selection");
            var category = CategoryOf(CurrentPage);
            if (category.HasValue)
            {
                var part = catalog.FindPart(category.Value, _draft.ChosenId(category.Value));
                var slot = ColourOf(category.Value);
                var colour = slot.HasValue ? catalog.FindColour(slot.Value, _draft.ColourId(slot.Value)) : null;
                var partName = part != null ? part.DisplayName : noneText;
                label.text = colour != null ? $"{partName}, {colour.DisplayName}" : partName;
            }
            else if (CurrentPage == Page.Outfit)
            {
                var top = catalog.FindColour(AppearanceColourSlot.Top, _draft.ColourId(AppearanceColourSlot.Top));
                var bottom = catalog.FindColour(AppearanceColourSlot.Bottom, _draft.ColourId(AppearanceColourSlot.Bottom));
                label.text = $"{top?.DisplayName}, {bottom?.DisplayName}";
            }
            else
            {
                label.text = _draft.Name;
            }
        }

        private void OnRandom()
        {
            _draft.Randomize(_random);
            Rebuild();
        }

        private void OnNameChanged(ChangeEvent<string> change)
        {
            _draft.Rename(change.newValue);
            ButtonNamed("start").SetEnabled(_draft.CanConfirm);
            ShowSelection();
        }

        private void OnStart()
        {
            if (_draft.CanConfirm)
            {
                eventBus.Publish(new CharacterCreated(_draft.Name.Trim(), _draft.Look));
            }
        }

        private void OnBack()
        {
            eventBus.Publish(new TitleScreenChosen());
        }

        // Q and E type letters while the name is being written.
        private bool Typing => _root.focusController?.focusedElement is VisualElement focused
            && (focused is TextField || focused.GetFirstAncestorOfType<TextField>() != null);

        private void OnPreviousTab(InputAction.CallbackContext context)
        {
            if (!Typing)
            {
                ShowPage((Page)(((int)CurrentPage + TabNames.Length - 1) % TabNames.Length));
            }
        }

        private void OnNextTab(InputAction.CallbackContext context)
        {
            if (!Typing)
            {
                ShowPage((Page)(((int)CurrentPage + 1) % TabNames.Length));
            }
        }
    }
}
