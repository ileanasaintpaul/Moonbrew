using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Npc
{
    // Serving a villager herself brings the owner closer to them; the employee's service does not.
    public class RelationBook : MonoBehaviour
    {
        [SerializeField] private VillagerCatalog villagers;
        [SerializeField] private EventBusAsset eventBus;

        private bool _subscribed;

        public Relationships Relationships { get; } = new Relationships();

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(VillagerCatalog catalog, EventBusAsset bus)
        {
            villagers = catalog;
            eventBus = bus;
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<OrderServed>(OnOrderServed);
            eventBus.Subscribe<GameLoaded>(OnLoaded);
            eventBus.Subscribe<NewGameStarted>(OnNewGame);
            eventBus.Subscribe<GameSaving>(OnSaving);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<OrderServed>(OnOrderServed);
            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
            eventBus.Unsubscribe<NewGameStarted>(OnNewGame);
            eventBus.Unsubscribe<GameSaving>(OnSaving);
            _subscribed = false;
        }

        private void OnOrderServed(OrderServed message)
        {
            if (message.ByEmployee || message.CustomerIds == null || villagers == null)
            {
                return;
            }

            foreach (var customerId in message.CustomerIds)
            {
                var villager = villagers.Find(customerId);
                if (villager != null && Relationships.Add(villager.Id, villager.ServedPoints))
                {
                    eventBus.Publish(new RelationChanged(villager.Id, Relationships.PointsWith(villager.Id), villager.ServedPoints));
                }
            }
        }

        private void OnLoaded(GameLoaded message)
        {
            Relationships.Restore(message.Data.relations);
        }

        private void OnNewGame(NewGameStarted message)
        {
            Relationships.Restore(message.Data.relations);
        }

        private void OnSaving(GameSaving message)
        {
            message.Data.relations = Relationships.ToSave();
        }
    }
}
