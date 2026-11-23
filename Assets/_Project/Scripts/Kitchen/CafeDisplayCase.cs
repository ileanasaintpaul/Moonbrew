using Moonbrew.Core;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Kitchen
{
    // The café's display case in the scene: a click from close by opens its panel, whose requests
    // put cakes in the case and take portions out. Its content is saved by its InventoryHolder.
    public class CafeDisplayCase : MonoBehaviour
    {
        [SerializeField] private DisplayCaseData data;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private InventoryHolder display;
        [SerializeField] private InventoryHolder ownerInventory;
        [SerializeField] private RecipeCatalog recipes;
        [SerializeField] private Transform player;

        private bool _subscribed;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(DisplayCaseData caseData, EventBusAsset bus, InventoryHolder stock, InventoryHolder owner, RecipeCatalog catalog, Transform playerTransform)
        {
            data = caseData;
            eventBus = bus;
            display = stock;
            ownerInventory = owner;
            recipes = catalog;
            player = playerTransform;
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<TargetClicked>(OnTargetClicked);
            eventBus.Subscribe<DisplayMoveRequested>(OnMoveRequested);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<TargetClicked>(OnTargetClicked);
            eventBus.Unsubscribe<DisplayMoveRequested>(OnMoveRequested);
            _subscribed = false;
        }

        private void OnTargetClicked(TargetClicked message)
        {
            if (message.Target == null || !message.Target.transform.IsChildOf(transform) || player == null)
            {
                return;
            }

            var offset = player.position - transform.position;
            offset.y = 0f;
            if (offset.magnitude <= data.Reach)
            {
                eventBus.Publish(new DisplayOpened());
            }
        }

        private void OnMoveRequested(DisplayMoveRequested message)
        {
            if (message.FromDisplay)
            {
                DisplayCase.Take(display.Inventory, message.Slot, ownerInventory.Inventory);
            }
            else
            {
                DisplayCase.Place(ownerInventory.Inventory, message.Slot, display.Inventory, recipes != null ? recipes.Recipes : null);
            }
        }
    }
}
