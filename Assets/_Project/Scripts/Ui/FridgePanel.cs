using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // The fridge or display case window: its slots above, the owner's café inventory below. Display only: it reads
    // both inventories and forwards the clicked slot (FridgeMoveRequested, DisplayMoveRequested). Pauses the game like the other menus.
    [RequireComponent(typeof(UIDocument))]
    public class FridgePanel : MonoBehaviour
    {
        private const string RootName = "fridge-panel";
        private const string FridgeGridName = "fridge-slots";
        private const string OwnerGridName = "owner-slots";
        private const string CloseName = "close";
        private const string SlotName = "slot";
        private const string IconName = "icon";
        private const string CountName = "count";
        private const string KeyName = "key";
        private const string WaterName = "water";

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private InventoryHolder fridge;
        [SerializeField] private InventoryHolder ownerInventory;
        [SerializeField] private VisualTreeAsset panelTemplate;
        [SerializeField] private VisualTreeAsset slotTemplate;
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private InventoryHolder displayCase;
        [SerializeField] private VisualTreeAsset displayTemplate;

        private readonly List<VisualElement> _fridgeSlots = new List<VisualElement>();
        private readonly List<VisualElement> _ownerSlots = new List<VisualElement>();
        private UIDocument _document;
        private VisualElement _panel;
        private InventoryHolder _stock;
        private bool _subscribed;

        public bool IsOpen => _panel != null;

        public string CountText(bool inFridge, int slot) => (inFridge ? _fridgeSlots : _ownerSlots)[slot].Q<Label>(CountName).text;

        public bool HasIcon(bool inFridge, int slot) => (inFridge ? _fridgeSlots : _ownerSlots)[slot].Q(IconName).style.backgroundImage.value.sprite != null;

        public void Init(EventBusAsset bus, InventoryHolder stock, InventoryHolder owner, VisualTreeAsset panel, VisualTreeAsset slot,
            InventoryHolder displayStock = null, VisualTreeAsset displayPanel = null)
        {
            Unsubscribe();
            eventBus = bus;
            fridge = stock;
            ownerInventory = owner;
            panelTemplate = panel;
            slotTemplate = slot;
            displayCase = displayStock;
            displayTemplate = displayPanel;
            Subscribe();
        }

        // Fridge: left click for the whole stack, right click for one item. Display case: one portion or one cake.
        // inFridge means the upper grid, whichever stock it shows. Also used by tests.
        public void ClickSlot(bool inFridge, int slot, bool wholeStack)
        {
            if (_stock != null && _stock == displayCase)
            {
                eventBus.Publish(new DisplayMoveRequested(inFridge, slot));
            }
            else
            {
                eventBus.Publish(new FridgeMoveRequested(inFridge, slot, wholeStack));
            }
        }

        public void Close()
        {
            if (!Remove())
            {
                return;
            }

            eventBus.Publish(new GameMenuToggled(false));
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Close();
            Unsubscribe();
        }

        private void OnFridgeOpened(FridgeOpened message)
        {
            Open(fridge, panelTemplate);
        }

        private void OnDisplayOpened(DisplayOpened message)
        {
            Open(displayCase, displayTemplate);
        }

        private void Open(InventoryHolder stock, VisualTreeAsset template)
        {
            var root = _document != null ? _document.rootVisualElement : null;
            if (IsOpen || root == null || stock == null || template == null || slotTemplate == null)
            {
                return;
            }

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            // Announced before the panel exists, so this panel does not take its own event for another menu.
            eventBus.Publish(new GameMenuToggled(true));
            _stock = stock;
            _panel = template.Instantiate().Q(RootName);
            _panel.Q<Button>(CloseName).clicked += Close;
            BuildSlots(_panel.Q(FridgeGridName), _fridgeSlots, stock.Inventory.SlotCount, true);
            BuildSlots(_panel.Q(OwnerGridName), _ownerSlots, ownerInventory.Inventory.SlotCount, false);
            root.Add(_panel);
            Refresh();
        }

        // Another menu opening takes over: it now holds the pause, and will release it when it closes.
        private void OnGameMenuToggled(GameMenuToggled message)
        {
            if (message.Open)
            {
                Remove();
            }
        }

        private void OnInventoryChanged(InventoryChanged message)
        {
            if (IsOpen)
            {
                Refresh();
            }
        }

        private bool Remove()
        {
            if (_panel == null)
            {
                return false;
            }

            _panel.RemoveFromHierarchy();
            _panel = null;
            _stock = null;
            _fridgeSlots.Clear();
            _ownerSlots.Clear();
            return true;
        }

        private void BuildSlots(VisualElement grid, List<VisualElement> slots, int count, bool inFridge)
        {
            for (var i = 0; i < count; i++)
            {
                var slot = slotTemplate.Instantiate().Q(SlotName);
                slot.Q(KeyName).style.display = DisplayStyle.None;
                slot.Q(WaterName).style.display = DisplayStyle.None;
                slot.pickingMode = PickingMode.Position;
                foreach (var child in slot.Children())
                {
                    child.pickingMode = PickingMode.Ignore;
                }

                var index = i;
                slot.RegisterCallback<PointerDownEvent>(pointer =>
                {
                    if (pointer.button <= 1)
                    {
                        ClickSlot(inFridge, index, pointer.button == 0);
                    }
                });
                grid.Add(slot);
                slots.Add(slot);
            }
        }

        private void Refresh()
        {
            Show(_fridgeSlots, _stock.Inventory);
            Show(_ownerSlots, ownerInventory.Inventory);
        }

        private static void Show(List<VisualElement> slots, ItemInventory inventory)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var stack = inventory.Slots[i];
                var icon = stack.IsEmpty ? null : stack.Item.Icon;
                slots[i].Q(IconName).style.backgroundImage = icon != null ? new StyleBackground(icon) : new StyleBackground(StyleKeyword.None);
                slots[i].Q<Label>(CountName).text = HotbarLabels.Count(stack);
            }
        }

        private void Subscribe()
        {
            if (_subscribed || eventBus == null || !isActiveAndEnabled)
            {
                return;
            }

            eventBus.Subscribe<FridgeOpened>(OnFridgeOpened);
            eventBus.Subscribe<DisplayOpened>(OnDisplayOpened);
            eventBus.Subscribe<GameMenuToggled>(OnGameMenuToggled);
            eventBus.Subscribe<InventoryChanged>(OnInventoryChanged);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && eventBus != null)
            {
                eventBus.Unsubscribe<FridgeOpened>(OnFridgeOpened);
                eventBus.Unsubscribe<DisplayOpened>(OnDisplayOpened);
                eventBus.Unsubscribe<GameMenuToggled>(OnGameMenuToggled);
                eventBus.Unsubscribe<InventoryChanged>(OnInventoryChanged);
            }

            _subscribed = false;
        }
    }
}
