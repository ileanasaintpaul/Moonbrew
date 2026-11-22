using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // The character creation screen: icon tabs along the top, and a panel per tab: the body gauges, then the face, hair, beard,
    // top, bottom and hat choices drawn on the owner, each with the colour that goes with it underneath, then the name.
    // The look in progress lives in CharacterDraft; this screen shows it on the preview model, zooms on the face for the pages
    // about it, and passes Start on.
    [RequireComponent(typeof(UIDocument))]
    public class CharacterCreation : MonoBehaviour
    {
        public enum Page
        {
            Body,
            Face,
            Hair,
            Beard,
            Top,
            Bottom,
            Hat,
            Name,
        }

        private const string ShownClass = "panel-page--shown";
        private const string SelectedTabClass = "tab--selected";
        private const string CellClass = "cell";
        private const string SelectedCellClass = "cell--selected";
        private const string SwatchClass = "swatch";
        private const string SelectedSwatchClass = "swatch--selected";
        private const string OriginalSwatchClass = "swatch--original";

        private static readonly string[] TabNames = { "tab-body", "tab-face", "tab-hair", "tab-beard", "tab-top", "tab-bottom", "tab-hat", "tab-name" };
        private static readonly string[] Titles = { "Body", "Face", "Hair", "Beard", "Top", "Bottom", "Hat", "Name" };
        private static readonly string[] SliderNames = { "shape-type", "shape-size", "shape-musculature" };

        // The colour chosen under each slot's page, in AppearanceColourSlot order.
        private static readonly string[] ColourTitles = { "Skin Colour", "Hair Colour", "Eyebrow Colour", "Facial Hair Colour", "Top Colour", "Bottom Colour" };

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

        private readonly List<Button> _cells = new List<Button>();
        private readonly List<string> _cellIds = new List<string>();
        private VisualElement _tabs;
        private VisualElement _root;
        private TextField _nameField;
        private CharacterDraft _draft;

        public Page CurrentPage { get; private set; }

        public CharacterDraft Draft => _draft;

        public int CellCount => _cells.Count;

        public Button CellAt(int index) => _cells[index];

        // The swatches of the colour shown under the current page.
        public Button SwatchAt(int index) => _root.Q("colours").Query<Button>().AtIndex(index);

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

            _root.Q("page-body").EnableInClassList(ShownClass, page == Page.Body);
            _root.Q("page-parts").EnableInClassList(ShownClass, CategoryOf(page).HasValue);
            _root.Q("page-name").EnableInClassList(ShownClass, page == Page.Name);
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
                case Page.Top: return AppearanceCategory.Top;
                case Page.Bottom: return AppearanceCategory.Bottom;
                case Page.Hat: return AppearanceCategory.Hat;
                default: return null;
            }
        }

        private static AppearanceColourSlot? ColourOf(Page page)
        {
            switch (page)
            {
                case Page.Body: return AppearanceColourSlot.Skin;
                case Page.Face: return AppearanceColourSlot.Brows;
                case Page.Hair: return AppearanceColourSlot.Hair;
                case Page.Beard: return AppearanceColourSlot.Beard;
                case Page.Top: return AppearanceColourSlot.Top;
                case Page.Bottom: return AppearanceColourSlot.Bottom;
                default: return null;
            }
        }

        private static AppearanceThumbnails.Framing FramingOf(Page page)
        {
            switch (page)
            {
                case Page.Top: return AppearanceThumbnails.Framing.Top;
                case Page.Bottom: return AppearanceThumbnails.Framing.Bottom;
                default: return AppearanceThumbnails.Framing.Head;
            }
        }

        // Redraws the current page: the thumbnails show the look as it is now.
        private void Rebuild()
        {
            _cells.Clear();
            _cellIds.Clear();
            var category = CategoryOf(CurrentPage);
            if (category.HasValue)
            {
                BuildCells(category.Value);
            }

            BuildSwatches(ColourOf(CurrentPage));
            preview.Apply(_draft.Look);
        }

        // A cell per part of the category, plus None for the optional ones; each shows the current look wearing that part.
        private void BuildCells(AppearanceCategory category)
        {
            var grid = _root.Q("grid");
            grid.Clear();
            if (AppearanceCatalog.IsOptional(category))
            {
                AddCell(grid, category, "", noneText, null);
            }

            var framing = FramingOf(CurrentPage);
            foreach (var part in catalog.PartsOf(category))
            {
                var look = _draft.Look;
                AppearanceCatalog.SetPartId(look, category, part.Id);
                AddCell(grid, category, part.Id, null, thumbnails.Draw(look, framing, _cells.Count));
            }

            MarkCells(category);
        }

        private void AddCell(VisualElement grid, AppearanceCategory category, string id, string text, RenderTexture thumbnail)
        {
            var cell = new Button { text = text ?? "" };
            cell.AddToClassList(CellClass);
            if (thumbnail != null)
            {
                cell.style.backgroundImage = Background.FromRenderTexture(thumbnail);
            }

            cell.clicked += () => OnCellClicked(category, id);
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

            _root.Q<Label>("counter").text = string.Format(counterFormat, _cellIds.IndexOf(chosen) + 1, _cells.Count);
        }

        private void OnCellClicked(AppearanceCategory category, string id)
        {
            if (_draft.Choose(category, id))
            {
                preview.Apply(_draft.Look);
                MarkCells(category);
            }
        }

        // Under the page: plain swatches of the colour that goes with it; an outfit's own colours have a swatch of their own.
        private void BuildSwatches(AppearanceColourSlot? slot)
        {
            var section = _root.Q("colour-section");
            section.EnableInClassList(ShownClass, slot.HasValue);
            var row = _root.Q("colours");
            row.Clear();
            if (!slot.HasValue)
            {
                return;
            }

            _root.Q<Label>("colour-title").text = ColourTitles[(int)slot.Value];
            foreach (var colour in catalog.ColoursOf(slot.Value))
            {
                var swatch = new Button();
                swatch.AddToClassList(SwatchClass);
                swatch.EnableInClassList(OriginalSwatchClass, colour.Original);
                swatch.EnableInClassList(SelectedSwatchClass, colour.Id == _draft.ColourId(slot.Value));
                swatch.style.backgroundColor = colour.Swatch;
                var id = colour.Id;
                var chosenSlot = slot.Value;
                swatch.clicked += () => OnColour(chosenSlot, id);
                row.Add(swatch);
            }
        }

        // The page's thumbnails show the colour too: they are drawn again.
        private void OnColour(AppearanceColourSlot slot, string id)
        {
            if (_draft.ChooseColour(slot, id))
            {
                Rebuild();
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
    }
}
