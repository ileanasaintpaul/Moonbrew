using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using UnityEngine;

namespace Moonbrew.Economy
{
    // Notes every dish the café sells in this scene, and hands them to the day's recap when the day is carried or saved.
    public class SalesBook : MonoBehaviour
    {
        [SerializeField] private ItemCatalog items;
        [SerializeField] private EventBusAsset eventBus;

        private readonly DayRecap _pending = new DayRecap();
        private readonly List<int> _prices = new List<int>();
        private bool _subscribed;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(ItemCatalog catalog, EventBusAsset bus)
        {
            items = catalog;
            eventBus = bus;
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<OrderServed>(OnOrderServed);
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
            eventBus.Unsubscribe<GameSaving>(OnSaving);
            _subscribed = false;
        }

        private void OnOrderServed(OrderServed message)
        {
            if (message.DishIds == null || message.DishIds.Count == 0)
            {
                return;
            }

            _prices.Clear();
            foreach (var dishId in message.DishIds)
            {
                var dish = items != null ? items.Find(dishId) : null;
                _prices.Add(dish != null ? dish.BasePrice : 0);
            }

            var shares = SaleSplit.Shares(_prices, message.Payment);
            for (var i = 0; i < shares.Length; i++)
            {
                _pending.AddSale(message.DishIds[i], shares[i], message.ByEmployee);
            }
        }

        private void OnSaving(GameSaving message)
        {
            message.Data.recap ??= new DayRecap();
            foreach (var sale in _pending.sales)
            {
                message.Data.recap.AddSale(sale.dishId, sale.money, sale.byEmployee, sale.count);
            }

            _pending.sales.Clear();
        }
    }
}
