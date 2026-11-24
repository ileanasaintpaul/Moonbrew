using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Player
{
    // The owner's health in the scene: hits take it away, the night gives it back, the save keeps it.
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerConfig settings;
        [SerializeField] private EventBusAsset eventBus;

        private bool _subscribed;

        public Health Health { get; private set; }

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
                Debug.LogError($"{nameof(PlayerHealth)} on {name} needs the player config and the event bus.", this);
                enabled = false;
                return;
            }

            Health = new Health(settings.MaxHealth);
        }

        private void OnEnable()
        {
            if (_subscribed || Health == null)
            {
                return;
            }

            eventBus.Subscribe<DamageTaken>(OnDamageTaken);
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

            eventBus.Unsubscribe<DamageTaken>(OnDamageTaken);
            eventBus.Unsubscribe<NightPassed>(OnNightPassed);
            eventBus.Unsubscribe<NewGameStarted>(OnNewGame);
            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
            eventBus.Unsubscribe<GameSaving>(OnSaving);
            _subscribed = false;
        }

        private void OnDamageTaken(DamageTaken message)
        {
            Health.Lose(message.Damage);
            Announce();
        }

        private void OnNightPassed(NightPassed message)
        {
            Health.Wake();
            Announce();
        }

        private void OnNewGame(NewGameStarted message)
        {
            Health.Restore(message.Data.health);
            Announce();
        }

        private void OnLoaded(GameLoaded message)
        {
            Health.Restore(message.Data.health);
            Announce();
        }

        private void OnSaving(GameSaving message)
        {
            message.Data.health = Health.Current;
        }

        private void Announce()
        {
            eventBus.Publish(new HealthChanged(Health.Current, Health.Max));
        }
    }
}
