using System.Collections.Generic;
using Moonbrew.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Moonbrew.Cafe
{
    // The café room during service: brings groups in through the door, seats them, shows when a table is
    // ready to order, and sees them out. The rules live in CafeService; this wires them to the clock and the scene.
    public class CafeFloor : MonoBehaviour
    {
        private const string IconChild = "Icon";

        [SerializeField] private CafeConfig config;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private CafeTable[] tables = new CafeTable[0];
        [Tooltip("Where customers come in and go out, on the NavMesh.")]
        [SerializeField] private Transform door;

        private readonly Dictionary<TableGroup, List<CafeCustomer>> _inside = new Dictionary<TableGroup, List<CafeCustomer>>();
        private readonly Dictionary<TableGroup, GameObject> _bubbles = new Dictionary<TableGroup, GameObject>();
        private System.Random _random;
        private int _lastMinutes = -1;
        private bool _closed;
        private bool _paused;
        private bool _subscribed;

        public CafeService Service { get; private set; }

        // The customers inside, for tests and debugging.
        public IEnumerable<CafeCustomer> Customers
        {
            get
            {
                foreach (var group in _inside.Values)
                {
                    foreach (var customer in group)
                    {
                        yield return customer;
                    }
                }
            }
        }

        public bool HasBubble(int table) => Service != null && Service.Tables[table] != null && _bubbles.ContainsKey(Service.Tables[table]);

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(CafeConfig cafeConfig, EventBusAsset bus, CafeTable[] cafeTables, Transform entrance, System.Random random)
        {
            config = cafeConfig;
            eventBus = bus;
            tables = cafeTables ?? new CafeTable[0];
            door = entrance;
            _random = random;
        }

        private void Awake()
        {
            if (config == null || eventBus == null || door == null || config.Customers == null)
            {
                Debug.LogError($"{nameof(CafeFloor)} on {name} needs a config with customers, the event bus and the door.", this);
                enabled = false;
                return;
            }

            var seats = new int[tables.Length];
            for (var i = 0; i < tables.Length; i++)
            {
                seats[i] = tables[i] != null ? tables[i].Seats.Count : 0;
            }

            Service = new CafeService(config, seats, config.Customers.Customers, _random ?? new System.Random());
        }

        private void OnEnable()
        {
            if (_subscribed || Service == null)
            {
                return;
            }

            eventBus.Subscribe<TimeChanged>(OnTimeChanged);
            eventBus.Subscribe<NightPassed>(OnNightPassed);
            eventBus.Subscribe<GameMenuToggled>(OnMenuToggled);
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
            eventBus.Unsubscribe<GameMenuToggled>(OnMenuToggled);
            _subscribed = false;
        }

        // Game time only moves forward within a day; the first time heard is just a starting point.
        private void OnTimeChanged(TimeChanged message)
        {
            if (_lastMinutes >= 0 && message.Minutes > _lastMinutes)
            {
                foreach (var group in Service.Advance(message.Minutes - _lastMinutes))
                {
                    Show(group);
                }
            }

            _lastMinutes = message.Minutes;
            if (!_closed && message.Minutes >= config.CloseMinute)
            {
                _closed = true;
                foreach (var group in Service.Close())
                {
                    Show(group);
                }
            }

            var arriving = Service.TryArrive(message.Minutes);
            if (arriving != null)
            {
                Bring(arriving);
            }
        }

        // The scene is reloaded every morning, but a café left open overnight is emptied anyway.
        private void OnNightPassed(NightPassed message)
        {
            foreach (var customers in _inside.Values)
            {
                foreach (var customer in customers)
                {
                    Destroy(customer.gameObject);
                }
            }

            foreach (var bubble in _bubbles.Values)
            {
                Destroy(bubble);
            }

            _inside.Clear();
            _bubbles.Clear();
            Service.Reset();
            _lastMinutes = -1;
            _closed = false;
        }

        private void OnMenuToggled(GameMenuToggled message)
        {
            _paused = message.Open;
            foreach (var customer in Customers)
            {
                customer.SetPaused(_paused);
            }
        }

        private void Bring(TableGroup group)
        {
            var seats = tables[group.Table].Seats;
            var customers = new List<CafeCustomer>();
            _inside[group] = customers;
            for (var i = 0; i < group.Members.Count; i++)
            {
                var member = group.Members[i];
                var model = Instantiate(member.Model, door.position, door.rotation, transform);
                model.name = member.Id;
                model.SetActive(true);
                model.AddComponent<NavMeshAgent>();
                var customer = model.AddComponent<CafeCustomer>();
                customer.Init(member, config.WalkSpeed, config.CustomerRadius, config.SitSeconds);
                customers.Add(customer);

                var seat = seats[i];
                customer.WalkTo(seat.position, () => customer.SitAt(seat, () =>
                {
                    if (group.Sit())
                    {
                        Show(group);
                    }
                }));
                customer.SetPaused(_paused);
            }

            Debug.Log($"[Cafe] {group.Members.Count} customer(s) come in, table {group.Table}");
        }

        private void Show(TableGroup group)
        {
            switch (group.State)
            {
                case GroupState.Ready:
                    ShowBubble(group);
                    break;
                case GroupState.Leaving:
                    HideBubble(group);
                    SeeOut(group);
                    break;
            }
        }

        private void SeeOut(TableGroup group)
        {
            if (!_inside.TryGetValue(group, out var customers))
            {
                return;
            }

            foreach (var customer in customers.ToArray())
            {
                customer.WalkTo(door.position, () =>
                {
                    customers.Remove(customer);
                    eventBus.Publish(new CustomerLeft(customer.Data.Id));
                    Destroy(customer.gameObject);
                    if (customers.Count == 0)
                    {
                        _inside.Remove(group);
                        Service.Remove(group);
                    }
                });
                customer.SetPaused(_paused);
            }
        }

        private void ShowBubble(TableGroup group)
        {
            if (config.BubblePrefab == null || _bubbles.ContainsKey(group))
            {
                return;
            }

            var top = 0f;
            foreach (var customer in _inside[group])
            {
                top = Mathf.Max(top, ModelBounds.Top(customer.gameObject, customer.transform.position.y));
            }

            var table = tables[group.Table].transform.position;
            var bubble = Instantiate(config.BubblePrefab, new Vector3(table.x, top + config.BubbleGap, table.z), config.BubblePrefab.transform.rotation, transform);
            bubble.SetActive(true);
            var icon = bubble.transform.Find(IconChild);
            if (icon != null && icon.TryGetComponent<SpriteRenderer>(out var iconRenderer))
            {
                iconRenderer.sprite = config.ReadyIcon;
            }

            _bubbles[group] = bubble;
        }

        private void HideBubble(TableGroup group)
        {
            if (_bubbles.TryGetValue(group, out var bubble))
            {
                Destroy(bubble);
                _bubbles.Remove(group);
            }
        }
    }
}
