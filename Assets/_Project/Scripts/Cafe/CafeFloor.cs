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
        [Tooltip("The owner, who has to stand near a table to take its order.")]
        [SerializeField] private Transform player;

        private readonly Dictionary<TableGroup, List<CafeCustomer>> _inside = new Dictionary<TableGroup, List<CafeCustomer>>();
        private readonly Dictionary<TableGroup, List<GameObject>> _bubbles = new Dictionary<TableGroup, List<GameObject>>();
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

        public bool HasBubble(int table) => BubbleCount(table) > 0;

        // One "!" bubble for a ready table, one bubble per dish once it has ordered.
        public int BubbleCount(int table)
        {
            var group = Service?.Tables[table];
            return group != null && _bubbles.TryGetValue(group, out var bubbles) ? bubbles.Count : 0;
        }

        // The bubbles of a table, for tests and debugging.
        public IReadOnlyList<GameObject> Bubbles(int table)
        {
            var group = Service?.Tables[table];
            return group != null && _bubbles.TryGetValue(group, out var bubbles) ? bubbles : new List<GameObject>();
        }

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(CafeConfig cafeConfig, EventBusAsset bus, CafeTable[] cafeTables, Transform entrance, System.Random random, Transform owner = null)
        {
            config = cafeConfig;
            eventBus = bus;
            tables = cafeTables ?? new CafeTable[0];
            door = entrance;
            _random = random;
            player = owner;
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
            eventBus.Subscribe<TargetClicked>(OnTargetClicked);
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
            eventBus.Unsubscribe<TargetClicked>(OnTargetClicked);
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
            foreach (var group in Service.Tables)
            {
                if (group != null && group.State == GroupState.Ordered)
                {
                    eventBus.Publish(new OrderAbandoned(group.Table));
                }
            }

            foreach (var customers in _inside.Values)
            {
                foreach (var customer in customers)
                {
                    Destroy(customer.gameObject);
                }
            }

            foreach (var bubbles in _bubbles.Values)
            {
                foreach (var bubble in bubbles)
                {
                    Destroy(bubble);
                }
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

        // A click on a table's "!" bubble, with the owner close enough, takes its order.
        private void OnTargetClicked(TargetClicked message)
        {
            if (_paused || message.Target == null || player == null)
            {
                return;
            }

            foreach (var pair in _bubbles)
            {
                var group = pair.Key;
                if (group.State != GroupState.Ready || !pair.Value.Contains(message.Target.gameObject))
                {
                    continue;
                }

                var table = tables[group.Table].transform.position;
                if (Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(table.x, table.z)) <= config.OrderReach)
                {
                    TakeOrder(group);
                }

                return;
            }
        }

        private void TakeOrder(TableGroup group)
        {
            var menu = new List<MenuDish>();
            eventBus.Publish(new MenuRequested(menu));
            if (!Service.TakeOrder(group, menu))
            {
                return;
            }

            HideBubble(group);
            ShowOrder(group);
            var dishIds = new List<string>();
            foreach (var dish in group.Order)
            {
                dishIds.Add(dish.Id);
            }

            eventBus.Publish(new OrderTaken(group.Table, dishIds));
            Debug.Log($"[Cafe] Table {group.Table} ordered: {string.Join(", ", dishIds)}");
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
                    if (group.Order.Count > 0)
                    {
                        eventBus.Publish(new OrderAbandoned(group.Table));
                    }

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

            _bubbles[group] = new List<GameObject> { SpawnBubble(BubbleCentre(group), config.ReadyIcon) };
        }

        // One bubble per dish, in a row across the screen, centred on the table.
        private void ShowOrder(TableGroup group)
        {
            if (config.BubblePrefab == null)
            {
                return;
            }

            var centre = BubbleCentre(group);
            var across = Camera.main != null ? Camera.main.transform.right : Vector3.right;
            var bubbles = new List<GameObject>();
            for (var i = 0; i < group.Order.Count; i++)
            {
                var offset = (i - (group.Order.Count - 1) / 2f) * config.OrderBubbleSpacing;
                bubbles.Add(SpawnBubble(centre + across * offset, group.Order[i].Icon));
            }

            _bubbles[group] = bubbles;
        }

        private Vector3 BubbleCentre(TableGroup group)
        {
            var top = 0f;
            foreach (var customer in _inside[group])
            {
                top = Mathf.Max(top, ModelBounds.Top(customer.gameObject, customer.transform.position.y));
            }

            var table = tables[group.Table].transform.position;
            return new Vector3(table.x, top + config.BubbleGap, table.z);
        }

        private GameObject SpawnBubble(Vector3 position, Sprite sprite)
        {
            var bubble = Instantiate(config.BubblePrefab, position, config.BubblePrefab.transform.rotation, transform);
            bubble.SetActive(true);
            var icon = bubble.transform.Find(IconChild);
            if (icon != null && icon.TryGetComponent<SpriteRenderer>(out var iconRenderer))
            {
                iconRenderer.sprite = sprite;
            }

            return bubble;
        }

        private void HideBubble(TableGroup group)
        {
            if (!_bubbles.TryGetValue(group, out var bubbles))
            {
                return;
            }

            foreach (var bubble in bubbles)
            {
                Destroy(bubble);
            }

            _bubbles.Remove(group);
        }
    }
}
