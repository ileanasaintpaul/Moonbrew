using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // The character creation screen: icon tabs along the top, and a panel per tab: the body gauges, the face, hair, beard,
    // outfit and hat choices drawn on the owner, the colours, then the name. The look in progress lives in CharacterDraft;
    // this screen shows it on the preview model, zooms on the face for the pages about it, and passes Start on.
    [RequireComponent(typeof(UIDocument))]
    public class CharacterCreation : MonoBehaviour
    {
        public enum Page
        {
            Body,
            Face,
            Hair,
            Beard,
            Outfit,
            Hat,
            Colours,
            Name,
        }

        private const string ShownClass = "panel-page--shown";
        private const string SelectedTabClass = "tab--selected";
        private const string CellClass = "cell";
        private const string SelectedCellClass = "cell--selected";
        private const string SwatchClass = "swatch";
        private const string SelectedSwatchClass = "swatch--selected";
        private const string OriginalSwatchClass = "swatch--original";

        private static readonly string[] TabNames = { "tab-body", "tab-face", "tab-hair", "tab-beard", "tab-outfit", "tab-hat", "tab-colours", "tab-name" };
        private static readonly string[] PageNames = { "page-body", "page-parts", "page-parts", "page-parts", "page-outfit", "page-parts", "page-colours", "page-name" };
        private static readonly string[] Titles = { "Body", "Face", "Hair", "Beard", "Outfit", "Hat", "Colours", "Name" };
        private static readonly string[] SliderNames = { "shape-type", "shape-size", "shape-musculature" };
        private static readonly string[] ColourRows = { "skin-colours", "hair-colours", "brow-colours", "beard-colours", "top-colours", "bottom-colours" };

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private AppearanceCatalog catalog;
        [Tooltip("The owner the player sees, dressed as the look changes.")]
        [SerializeField] private OwnerAppearance preview;
        [SerializeField] private AppearanceThumbnails thumbnails;
        [Tooltip("The panel's document, drawn in perspective on a quad of the scene (see WorldPanel).")]
        [SerializeField] private UIDocument panel;
        [Tooltip("Optional: glides to the face for the face, hair, beard and hat pages.")]
        [SerializeField] private CreationCamera creationCamera;
        [SerializeField] private string noneText = "None";
        [SerializeField] private string counterFormat = "{0} / {1}";
        [SerializeField] private InputAction previousTab = new InputAction("Previous Tab", InputActionType.Button, "<Keyboard>/q");
        [SerializeField] private InputAction nextTab = new InputAction("Next Tab", InputActionType.Button, "<Keyboard>/e");

        private readonly Dictionary<AppearanceCategory, Cells> _cells = new Dictionary<AppearanceCategory, Cells>();
        private VisualElement _tabs;
        private VisualElement _root;
        private TextField _nameField;
        private CharacterDraft _draft;
        private int _thumbnail;

        public Page CurrentPage { get; private set; }

        public CharacterDraft Draft => _draft;

        public int CellCount(AppearanceCategory category) => _cells.TryGetValue(category, out var cells) ? cells.Buttons.Count : 0;

        public Button CellAt(AppearanceCategory category, int index) => _cells[category].Buttons[index];

        public Button SwatchAt(AppearanceColourSlot slot, int index) => _root.Q(ColourRows[(int)slot]).Query<Button>().AtIndex(index);

        public SliderInt SliderOf(BodyShape shape) => _root.Q<SliderInt>(SliderNames[(int)shape]);

        public Button ButtonNamed(string buttonName) => _root?.Q<Button>(buttonName) ?? _tabs?.Q<Button>(buttonName);

        public TextField NameField => _nameField;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(EventBusAsset bus, AppearanceCatalog looks, OwnerAppearance previewModel, AppearanceThumbnails thumbnailRenderer, UIDocument panelDocument, CreationCamera zoom = null)
        {
            eventBus = bus;
            catalog = looks;
            preview = previewModel;
            thumbnails = thumbnailRenderer;
            panel = panelDocument;
            creationCamera = zoom;
        }

        public void ShowPage(Page page)
        {
            CurrentPage = page;
            for (var i = 0; i < TabNames.Length; i++)
            {
                _tabs.Q<Button>(TabNames[i]).EnableInClassList(SelectedTabClass, i == (int)page);
            }

            foreach (var pageName in PageNames)
            {
                _root.Q(pageName).RemoveFromClassList(ShownClass);
            }

            _root.Q(PageNames[(int)page]).AddToClassList(ShownClass);
            _root.Q<Label>("panel-title").text = Titles[(int)page];
            if (creationCamera != null)
            {
                creationCamera.ShowFace(page == Page.Face || page == Page.Hair || page == Page.Beard || page == Page.Hat);
            }

            Rebuild();
        }

        private void Awake()
        {
            if (eventBus == null || catalog == null || preview == null || thumbnails == null || panel == null)
            {
                Debug.LogError($"{nameof(CharacterCreation)} on {name} needs the event bus, the catalog, the preview model, the thumbnails and the panel.", this);
                enabled = false;
                return;
            }

            _draft = new CharacterDraft(catalog);
        }

        private void OnEnable()
        {
            _tabs = GetComponent<UIDocument>().rootVisualElement;
            _root = panel.rootVisualElement;
            for (var i = 0; i < TabNames.Length; i++)
            {
                var page = (Page)i;
                _tabs.Q<Button>(TabNames[i]).clicked += () => ShowPage(page);
            }

            for (var i = 0; i < SliderNames.Length; i++)
            {
                var shape = (BodyShape)i;
                var slider = _root.Q<SliderInt>(SliderNames[i]);
                slider.lowValue = -AppearanceCatalog.ShapeLimit;
                slider.highValue = AppearanceCatalog.ShapeLimit;
                slider.RegisterValueChangedCallback(change => OnShapeChanged(shape, change.newValue));
            }

            _root.Q<Button>("start").clicked += OnStart;
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
            for (var i = 0; i < SliderNames.Length; i++)
            {
                SliderOf((BodyShape)i).SetValueWithoutNotify(_draft.ShapeValue((BodyShape)i));
            }

            preview.Apply(_draft.Look);
            ButtonNamed("start").SetEnabled(_draft.CanConfirm);
            ShowPage(Page.Body);
        }

        private static AppearanceCategory? CategoryOf(Page page)
        {
            switch (page)
            {
                case Page.Face: return AppearanceCategory.Brows;
                case Page.Hair: return AppearanceCategory.Hair;
                case Page.Beard: return AppearanceCategory.Beard;
                case Page.Hat: return AppearanceCategory.Hat;
                default: return null;
            }
        }

        // Redraws the current page: the thumbnails show the look as it is now.
        private void Rebuild()
        {
            _thumbnail = 0;
            _cells.Clear();
            var category = CategoryOf(CurrentPage);
            if (category.HasValue)
            {
                BuildCells(_root.Q("grid"), category.Value, AppearanceThumbnails.Framing.Head);
                ShowCounter(category.Value);
            }
            else if (CurrentPage == Page.Outfit)
            {
                BuildCells(_root.Q("top-grid"), AppearanceCategory.Top, AppearanceThumbnails.Framing.Top);
                BuildCells(_root.Q("bottom-grid"), AppearanceCategory.Bottom, AppearanceThumbnails.Framing.Bottom);
            }
            else if (CurrentPage == Page.Colours)
            {
                BuildSwatches();
            }

            preview.Apply(_draft.Look);
        }

        // A cell per part of the category, plus None for the optional ones; each shows the current look wearing that part.
        private void BuildCells(VisualElement grid, AppearanceCategory category, AppearanceThumbnails.Framing framing)
        {
            grid.Clear();
            var cells = new Cells();
            _cells[category] = cells;
            if (AppearanceCatalog.IsOptional(category))
            {
                AddCell(grid, cells, category, "", noneText, null);
            }

            foreach (var part in catalog.PartsOf(category))
            {
                var look = _draft.Look;
                AppearanceCatalog.SetPartId(look, category, part.Id);
                AddCell(grid, cells, category, part.Id, null, thumbnails.Draw(look, framing, _thumbnail++));
            }

            MarkCells(category);
        }

        private void AddCell(VisualElement grid, Cells cells, AppearanceCategory category, string id, string text, RenderTexture thumbnail)
        {
            var cell = new Button { text = text ?? "" };
            cell.AddToClassList(CellClass);
            if (thumbnail != null)
            {
                cell.style.backgroundImage = Background.FromRenderTexture(thumbnail);
            }

            cell.clicked += () => OnCellClicked(category, id);
            grid.Add(cell);
            cells.Buttons.Add(cell);
            cells.Ids.Add(id);
        }

        private void MarkCells(AppearanceCategory category)
        {
            var cells = _cells[category];
            var chosen = _draft.ChosenId(category);
            for (var i = 0; i < cells.Buttons.Count; i++)
            {
                cells.Buttons[i].EnableInClassList(SelectedCellClass, cells.Ids[i] == chosen);
            }
        }

        private void ShowCounter(AppearanceCategory category)
        {
            var cells = _cells[category];
            var index = cells.Ids.IndexOf(_draft.ChosenId(category));
            _root.Q<Label>("counter").text = string.Format(counterFormat, index + 1, cells.Buttons.Count);
        }

        private void OnCellClicked(AppearanceCategory category, string id)
        {
            if (_draft.Choose(category, id))
            {
                preview.Apply(_draft.Look);
                MarkCells(category);
                if (CategoryOf(CurrentPage) == category)
                {
                    ShowCounter(category);
                }
            }
        }

        // A row of plain swatches per colour; an outfit's own colours have a swatch of their own.
        private void BuildSwatches()
        {
            for (var i = 0; i < ColourRows.Length; i++)
            {
                var slot = (AppearanceColourSlot)i;
                var row = _root.Q(ColourRows[i]);
                row.Clear();
                foreach (var colour in catalog.ColoursOf(slot))
                {
                    var swatch = new Button();
                    swatch.AddToClassList(SwatchClass);
                    swatch.EnableInClassList(OriginalSwatchClass, colour.Original);
                    swatch.EnableInClassList(SelectedSwatchClass, colour.Id == _draft.ColourId(slot));
                    swatch.style.backgroundColor = colour.Swatch;
                    var id = colour.Id;
                    swatch.clicked += () => OnColour(slot, id);
                    row.Add(swatch);
                }
            }
        }

        private void OnColour(AppearanceColourSlot slot, string id)
        {
            if (_draft.ChooseColour(slot, id))
            {
                preview.Apply(_draft.Look);
                var row = _root.Q(ColourRows[(int)slot]);
                var colours = catalog.ColoursOf(slot);
                var swatches = row.Query<Button>().ToList();
                for (var i = 0; i < swatches.Count && i < colours.Count; i++)
                {
                    swatches[i].EnableInClassList(SelectedSwatchClass, colours[i].Id == id);
                }
            }
        }

        private void OnShapeChanged(BodyShape shape, int value)
        {
            _draft.SetShape(shape, value);
            preview.Apply(_draft.Look);
        }

        private void OnNameChanged(ChangeEvent<string> change)
        {
            _draft.Rename(change.newValue);
            ButtonNamed("start").SetEnabled(_draft.CanConfirm);
        }

        private void OnStart()
        {
            if (_draft.CanConfirm)
            {
                eventBus.Publish(new CharacterCreated(_draft.Name.Trim(), _draft.Look));
            }
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

        private class Cells
        {
            public readonly List<Button> Buttons = new List<Button>();
            public readonly List<string> Ids = new List<string>();
        }
    }
}
