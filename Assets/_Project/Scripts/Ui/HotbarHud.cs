using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // The hotbar at the bottom of the screen: the first slots of the player's bag, with the selected one highlighted.
    // Display only: it reads the bag and the hotbar, and refreshes on InventoryChanged and HotbarSlotSelected.
    [RequireComponent(typeof(UIDocument))]
    public class HotbarHud : MonoBehaviour
    {
        private const string BarClass = "hotbar";
        private const string SelectedClass = "hotbar-slot--selected";
        private const string SlotName = "slot";
        private const string IconName = "icon";
        private const string CountName = "count";
        private const string KeyName = "key";

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private PlayerHotbar hotbar;
        [SerializeField] private InventoryHolder bag;
        [SerializeField] private VisualTreeAsset slotTemplate;
        [SerializeField] private StyleSheet styleSheet;

        private readonly List<VisualElement> _slots = new List<VisualElement>();
        private UIDocument _document;
        private VisualElement _bar;
        private bool _subscribed;

        public int VisibleSlots => _slots.Count;

        public int HighlightedIndex => _slots.FindIndex(slot => slot.ClassListContains(SelectedClass));

        public string CountText(int index)
        {
            return _slots[index].Q<Label>(CountName).text;
        }

        public bool HasIcon(int index)
        {
            return _slots[index].Q(IconName).style.backgroundImage.value.sprite != null;
        }

        // A click on a slot: forwarded to the player's hotbar, which announces the new selection.
        public void ClickSlot(int index)
        {
            if (hotbar != null)
            {
                hotbar.Select(index);
            }
        }

        public void Init(EventBusAsset bus, PlayerHotbar playerHotbar, InventoryHolder playerBag, VisualTreeAsset template)
        {
            Unsubscribe();
            eventBus = bus;
            hotbar = playerHotbar;
            bag = playerBag;
            slotTemplate = template;
            Subscribe();
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
            Unsubscribe();
            _bar?.RemoveFromHierarchy();
            _bar = null;
            _slots.Clear();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnInventoryChanged(InventoryChanged message)
        {
            Refresh();
        }

        private void OnHotbarSlotSelected(HotbarSlotSelected message)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (hotbar == null || hotbar.Hotbar == null || !EnsureBar())
            {
                return;
            }

            var bagSlots = bag != null && bag.Inventory != null ? bag.Inventory.Slots : null;
            for (var i = 0; i < _slots.Count; i++)
            {
                var stack = bagSlots != null && i < bagSlots.Count ? bagSlots[i] : default;
                var icon = stack.IsEmpty ? null : stack.Item.Icon;
                _slots[i].Q(IconName).style.backgroundImage = icon != null ? new StyleBackground(icon) : new StyleBackground(StyleKeyword.None);
                _slots[i].Q<Label>(CountName).text = HotbarLabels.Count(stack);
                _slots[i].EnableInClassList(SelectedClass, i == hotbar.Hotbar.SelectedIndex);
            }
        }

        // The UIDocument builds its root when enabled, which may come after this component: build the bar lazily.
        private bool EnsureBar()
        {
            if (_bar != null)
            {
                return true;
            }

            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null || slotTemplate == null)
            {
                return false;
            }

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            _bar = new VisualElement();
            _bar.AddToClassList(BarClass);
            _bar.pickingMode = PickingMode.Ignore;
            for (var i = 0; i < hotbar.Hotbar.SlotCount; i++)
            {
                // Take the slot out of its TemplateContainer so the bar's flex row lays the slots out directly.
                var slot = slotTemplate.Instantiate().Q(SlotName);
                slot.Q<Label>(KeyName).text = HotbarLabels.Key(i);

                // The whole slot catches the click, not its icon or labels.
                slot.pickingMode = PickingMode.Position;
                foreach (var child in slot.Children())
                {
                    child.pickingMode = PickingMode.Ignore;
                }

                var index = i;
                slot.RegisterCallback<PointerDownEvent>(pointer =>
                {
                    if (pointer.button == 0)
                    {
                        ClickSlot(index);
                    }
                });
                _bar.Add(slot);
                _slots.Add(slot);
            }

            root.Add(_bar);
            return true;
        }

        private void Subscribe()
        {
            if (_subscribed || eventBus == null || !isActiveAndEnabled)
            {
                return;
            }

            eventBus.Subscribe<InventoryChanged>(OnInventoryChanged);
            eventBus.Subscribe<HotbarSlotSelected>(OnHotbarSlotSelected);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && eventBus != null)
            {
                eventBus.Unsubscribe<InventoryChanged>(OnInventoryChanged);
                eventBus.Unsubscribe<HotbarSlotSelected>(OnHotbarSlotSelected);
            }

            _subscribed = false;
        }
    }
}
