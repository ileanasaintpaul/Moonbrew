using Moonbrew.Core;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Kitchen
{
    // Answers the employee's requests from the café's fridge and display case.
    public class CafePantry : MonoBehaviour
    {
        [SerializeField] private InventoryHolder fridge;
        [SerializeField] private InventoryHolder display;
        [SerializeField] private RecipeCatalog recipes;
        [SerializeField] private ItemCatalog items;
        [SerializeField] private EventBusAsset eventBus;

        private bool _subscribed;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(InventoryHolder fridgeHolder, InventoryHolder displayHolder, RecipeCatalog recipeCatalog, ItemCatalog itemCatalog, EventBusAsset bus)
        {
            fridge = fridgeHolder;
            display = displayHolder;
            recipes = recipeCatalog;
            items = itemCatalog;
            eventBus = bus;
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<DishesRequested>(OnDishesRequested);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<DishesRequested>(OnDishesRequested);
            _subscribed = false;
        }

        private void OnDishesRequested(DishesRequested message)
        {
            if (fridge == null || display == null || recipes == null || items == null)
            {
                return;
            }

            message.Request.Prepared = EmployeeKitchen.Prepare(message.Request.DishIds, message.Request.MaxStars,
                fridge.Inventory, display.Inventory, recipes.Recipes, items.Find);
        }
    }
}
