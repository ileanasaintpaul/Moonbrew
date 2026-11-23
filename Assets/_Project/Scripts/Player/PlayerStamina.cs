using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Player
{
    // The owner's stamina in the scene: tools spend it, the night restores it, the save and the walk between scenes keep it.
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField] private PlayerConfig settings;
        [SerializeField] private EventBusAsset eventBus;

        private bool _subscribed;

        public Stamina Stamina { get; private set; }

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(PlayerConfig playerConfig, EventBusAsset bus)
        {
            settings = playerConfig;
            eventBus = bus;
        }

        private void Awake()
        {
            if (settings == null || eventBus == null)
            {
                Debug.LogError($"{nameof(PlayerStamina)} on {name} needs the player config and the event bus.", this);
                enabled = false;
                return;
            }

            Stamina = new Stamina(settings.MaxStamina);
        }

        private void OnEnable()
        {
            if (_subscribed || Stamina == null)
            {
                return;
            }

            eventBus.Subscribe<ToolUsed>(OnToolUsed);
            eventBus.Subscribe<NightPassed>(OnNightPassed);
            eventBus.Subscribe<NewGameStarted>(OnNewGame);
            eventBus.Subscribe<GameLoaded>(OnLoaded);
            eventBus.Subscribe<GameSaving>(OnSaving);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<ToolUsed>(OnToolUsed);
            eventBus.Unsubscribe<NightPassed>(OnNightPassed);
            eventBus.Unsubscribe<NewGameStarted>(OnNewGame);
            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
            eventBus.Unsubscribe<GameSaving>(OnSaving);
            _subscribed = false;
        }

        private void OnToolUsed(ToolUsed message)
        {
            Stamina.Spend(message.StaminaCost);
            Announce();
        }

        private void OnNightPassed(NightPassed message)
        {
            Stamina.Wake(message.Slept, settings.MissedSleepPenalty);
            Announce();
        }

        private void OnNewGame(NewGameStarted message)
        {
            Stamina.Restore(message.Data.stamina);
            Announce();
        }

        private void OnLoaded(GameLoaded message)
        {
            Stamina.Restore(message.Data.stamina);
            Announce();
        }

        private void OnSaving(GameSaving message)
        {
            message.Data.stamina = Stamina.Current;
        }

        private void Announce()
        {
            eventBus.Publish(new StaminaChanged(Stamina.Current, Stamina.Max));
        }
    }
}
