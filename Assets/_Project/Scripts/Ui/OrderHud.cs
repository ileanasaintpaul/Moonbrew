using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // Reminds the player of the orders taken: one row per table, with the icon of each dish. Display only.
    [RequireComponent(typeof(UIDocument))]
    public class OrderHud : MonoBehaviour
    {
        private const string ListClass = "order-hud";
        private const string RowClass = "order-hud__row";
        private const string LabelClass = "order-hud__table";
        private const string IconClass = "order-hud__icon";

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private ItemCatalog items;
        [SerializeField] private StyleSheet styleSheet;
        [Tooltip("{0} = table number, from 1.")]
        [SerializeField] private string tableFormat = "Table {0}";

        private readonly SortedDictionary<int, IReadOnlyList<string>> _orders = new SortedDictionary<int, IReadOnlyList<string>>();
        private UIDocument _document;
        private VisualElement _list;
        private bool _subscribed;

        public int RowCount => _list?.childCount ?? 0;

        // The number of dish icons on a row.
        public int IconCount(int row) => _list != null && row >= 0 && row < _list.childCount ? _list[row].childCount - 1 : 0;

        public void Init(EventBusAsset bus, ItemCatalog catalog)
        {
            Unsubscribe();
            eventBus = bus;
            items = catalog;
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
            _list?.RemoveFromHierarchy();
            _list = null;
        }

        private void OnOrderTaken(OrderTaken message)
        {
            _orders[message.Table] = message.DishIds;
            Refresh();
        }

        private void OnOrderAbandoned(OrderAbandoned message)
        {
            _orders.Remove(message.Table);
            Refresh();
        }

        private void Refresh()
        {
            if (!EnsureList())
            {
                return;
            }

            _list.Clear();
            foreach (var order in _orders)
            {
                var row = Part(new VisualElement(), RowClass);
                var label = Part(new Label(string.Format(tableFormat, order.Key + 1)), LabelClass);
                row.Add(label);
                foreach (var dishId in order.Value)
                {
                    var icon = Part(new VisualElement(), IconClass);
                    var dish = items != null ? items.Find(dishId) : null;
                    if (dish != null && dish.Icon != null)
                    {
                        icon.style.backgroundImage = new StyleBackground(dish.Icon);
                    }

                    row.Add(icon);
                }

                _list.Add(row);
            }

            _list.style.display = _orders.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // The UIDocument builds its root when enabled, which may come after this component.
        private bool EnsureList()
        {
            if (_list != null)
            {
                return true;
            }

            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null)
            {
                return false;
            }

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            _list = Part(new VisualElement(), ListClass);
            root.Add(_list);
            return true;
        }

        private static T Part<T>(T element, string className) where T : VisualElement
        {
            element.pickingMode = PickingMode.Ignore;
            element.AddToClassList(className);
            return element;
        }

        private void Subscribe()
        {
            if (_subscribed || eventBus == null || !isActiveAndEnabled)
            {
                return;
            }

            eventBus.Subscribe<OrderTaken>(OnOrderTaken);
            eventBus.Subscribe<OrderAbandoned>(OnOrderAbandoned);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && eventBus != null)
            {
                eventBus.Unsubscribe<OrderTaken>(OnOrderTaken);
                eventBus.Unsubscribe<OrderAbandoned>(OnOrderAbandoned);
            }

            _subscribed = false;
        }
    }
}
