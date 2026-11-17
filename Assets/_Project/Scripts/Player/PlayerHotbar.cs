using Moonbrew.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moonbrew.Player
{
    // Number keys select a hotbar slot; the choice is announced on the bus as HotbarSlotSelected.
    public class PlayerHotbar : MonoBehaviour
    {
        [SerializeField] private PlayerConfig settings;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private InputActionReference slotAction;

        public Hotbar Hotbar { get; private set; }

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(PlayerConfig playerConfig, EventBusAsset bus)
        {
            settings = playerConfig;
            eventBus = bus;
        }

        public void Select(int index)
        {
            if (Hotbar != null && Hotbar.Select(index))
            {
                Announce();
            }
        }

        private void Awake()
        {
            if (settings == null)
            {
                Debug.LogError($"{nameof(PlayerHotbar)} on {name} has no {nameof(PlayerConfig)} assigned.", this);
                enabled = false;
                return;
            }

            Hotbar = new Hotbar(settings.HotbarSlots);
        }

        private void OnEnable()
        {
            if (slotAction != null)
            {
                slotAction.action.performed += OnSlotKey;
                slotAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (slotAction != null)
            {
                slotAction.action.performed -= OnSlotKey;
            }
        }

        // So the HUD shows the starting selection.
        private void Start()
        {
            Announce();
        }

        private void OnSlotKey(InputAction.CallbackContext context)
        {
            if (Hotbar.SelectFromKey(context.ReadValue<float>()))
            {
                Announce();
            }
        }

        private void Announce()
        {
            if (eventBus != null)
            {
                eventBus.Publish(new HotbarSlotSelected(Hotbar.SelectedIndex));
            }
        }
    }
}
