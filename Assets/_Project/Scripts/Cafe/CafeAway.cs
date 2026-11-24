using System.Collections.Generic;
using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Cafe
{
    // Keeps the café running from a scene where its room is not loaded.
    public class CafeAway : MonoBehaviour
    {
        [SerializeField] private CafeConfig config;
        [SerializeField] private CafeDay cafeDay;
        [SerializeField] private EventBusAsset eventBus;

        private readonly List<TableGroup> _paid = new List<TableGroup>();
        private CafeRun _run;
        private bool _subscribed;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(CafeConfig cafeConfig, CafeDay day, EventBusAsset bus)
        {
            config = cafeConfig;
            cafeDay = day;
            eventBus = bus;
        }

        private void Awake()
        {
            if (config == null || cafeDay == null || eventBus == null)
            {
                Debug.LogError($"{nameof(CafeAway)} on {name} needs the café config, the café day and the event bus.", this);
                enabled = false;
                return;
            }

            _run = cafeDay.Run(config, config.TableSeats);
        }

        private void OnEnable()
        {
            if (_subscribed || _run == null)
            {
                return;
            }

            eventBus.Subscribe<TimeChanged>(OnTimeChanged);
            eventBus.Subscribe<NightPassed>(OnNightPassed);
            eventBus.Subscribe<GameLoaded>(OnLoaded);
            eventBus.Subscribe<NewGameStarted>(OnNewGame);
            eventBus.Subscribe<EmployeeFreed>(OnEmployeeFreed);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<TimeChanged>(OnTimeChanged);
            eventBus.Unsubscribe<NightPassed>(OnNightPassed);
            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
            eventBus.Unsubscribe<NewGameStarted>(OnNewGame);
            eventBus.Unsubscribe<EmployeeFreed>(OnEmployeeFreed);
            _subscribed = false;
        }

        private void OnTimeChanged(TimeChanged message)
        {
            _paid.Clear();
            foreach (var group in _run.AdvanceAway(message.Minutes, _paid))
            {
                foreach (var member in group.Members)
                {
                    eventBus.Publish(new CustomerLeft(member.Id));
                }
            }

            foreach (var group in _paid)
            {
                Debug.Log($"[Cafe] The employee cashed table {group.Table}: {group.Bill}");
                eventBus.Publish(new BillPaid(group.Table, group.DishIds(), group.Bill, group.ServedByEmployee));
            }
        }

        // One table per free moment: takes the order of a ready table, then serves it if the kitchen can make it all.
        private void OnEmployeeFreed(EmployeeFreed message)
        {
            foreach (var group in _run.Service.Tables)
            {
                if (group == null)
                {
                    continue;
                }

                if (group.State == GroupState.Ready)
                {
                    var menu = new List<MenuDish>();
                    eventBus.Publish(new MenuRequested(menu, message.MaxStars));
                    _run.Service.TakeOrder(group, menu);
                }

                if (group.State == GroupState.Ordered && TryServe(group, message.MaxStars))
                {
                    return;
                }
            }
        }

        private bool TryServe(TableGroup group, int maxStars)
        {
            var dishIds = new List<string>();
            foreach (var dish in group.Order)
            {
                dishIds.Add(dish.Id);
            }

            var request = new DishRequest(dishIds, maxStars);
            eventBus.Publish(new DishesRequested(request));
            if (!request.Prepared || !_run.Service.ServePrepared(group, out var payment))
            {
                return false;
            }

            Debug.Log($"[Cafe] The employee served table {group.Table}, bill {payment}");
            var customerIds = new List<string>();
            foreach (var member in group.Members)
            {
                customerIds.Add(member.Id);
            }

            eventBus.Publish(new OrderServed(group.Table, dishIds, payment, true, customerIds));
            return true;
        }

        private void OnNightPassed(NightPassed message)
        {
            _run.Reset();
        }

        private void OnLoaded(GameLoaded message)
        {
            if (!message.SameDay)
            {
                _run.Reset();
            }
        }

        private void OnNewGame(NewGameStarted message)
        {
            _run.Reset();
        }
    }
}
