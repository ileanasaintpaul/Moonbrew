using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Economy
{
    // The player's money in the scene: collects what tables pay, travels with the save and announces every change.
    public class WalletHolder : MonoBehaviour
    {
        [SerializeField] private EventBusAsset eventBus;

        private bool _subscribed;

        public Wallet Wallet { get; } = new Wallet();

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(EventBusAsset bus)
        {
            eventBus = bus;
        }

        public bool Spend(int amount)
        {
            if (!Wallet.Spend(amount))
            {
                return false;
            }

            eventBus.Publish(new MoneyChanged(Wallet.Balance, -amount));
            return true;
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<NewGameStarted>(OnNewGame);
            eventBus.Subscribe<GameLoaded>(OnLoaded);
            eventBus.Subscribe<GameSaving>(OnSaving);
            eventBus.Subscribe<OrderServed>(OnOrderServed);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<NewGameStarted>(OnNewGame);
            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
            eventBus.Unsubscribe<GameSaving>(OnSaving);
            eventBus.Unsubscribe<OrderServed>(OnOrderServed);
            _subscribed = false;
        }

        private void OnNewGame(NewGameStarted message)
        {
            Restore(message.Data.money);
        }

        private void OnLoaded(GameLoaded message)
        {
            Restore(message.Data.money);
        }

        private void Restore(int balance)
        {
            Wallet.Restore(balance);
            eventBus.Publish(new MoneyChanged(Wallet.Balance, 0));
        }

        private void OnSaving(GameSaving message)
        {
            message.Data.money = Wallet.Balance;
        }

        private void OnOrderServed(OrderServed message)
        {
            var before = Wallet.Balance;
            if (Wallet.Earn(message.Payment))
            {
                eventBus.Publish(new MoneyChanged(Wallet.Balance, Wallet.Balance - before));
            }
        }
    }
}
