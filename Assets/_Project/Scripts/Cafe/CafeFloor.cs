using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
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
        [Tooltip("The owner's café inventory, where the dishes of a served order come from.")]
        [SerializeField] private InventoryHolder ownerInventory;
        [Tooltip("The café's day, shared with the scenes where the room is not loaded.")]
        [SerializeField] private CafeDay cafeDay;
        [Tooltip("The till: a click on it, with the owner close enough, cashes the first bill of the queue. Needs a ClickTarget and a collider.")]
        [SerializeField] private Transform till;
        [Tooltip("Where the first customer of the queue stands, facing forward; the others line up behind.")]
        [SerializeField] private Transform tillQueue;

        private readonly Dictionary<TableGroup, List<CafeCustomer>> _inside = new Dictionary<TableGroup, List<CafeCustomer>>();
        private readonly Dictionary<TableGroup, List<GameObject>> _bubbles = new Dictionary<TableGroup, List<GameObject>>();
        private readonly List<TableGroup> _queue = new List<TableGroup>();
        private System.Random _random;
        private CafeRun _run;
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

        // Groups waiting at the till, first in line first.
        public IReadOnlyList<TableGroup> Queue => _queue;

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
        public void Init(CafeConfig cafeConfig, EventBusAsset bus, CafeTable[] cafeTables, Transform entrance, System.Random random, Transform owner = null,
            InventoryHolder ownerItems = null, CafeDay day = null)
        {
            cafeDay = day;
            ownerInventory = ownerItems;
            config = cafeConfig;
            eventBus = bus;
            tables = cafeTables ?? new CafeTable[0];
            door = entrance;
            _random = random;
            player = owner;
        }

        // For objects built from code: the till and where its queue starts.
        public void InitTill(Transform cafeTill, Transform queueStart)
        {
            till = cafeTill;
            tillQueue = queueStart;
        }

        // The owner cashes the first bill of the queue; also used by tests. Returns false when nobody waits to pay.
        public bool CashFirst()
        {
            return _queue.Count > 0 && Cash(_queue[0]);
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

            _run = cafeDay != null ? cafeDay.Run(config, seats) : new CafeRun(config, seats, _random ?? new System.Random());
            Service = _run.Service;
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
            eventBus.Subscribe<GameLoaded>(OnLoaded);
            eventBus.Subscribe<NewGameStarted>(OnNewGame);
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
            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
            eventBus.Unsubscribe<NewGameStarted>(OnNewGame);
            _subscribed = false;
        }

        // Back from another scene: the room shows the café as it went on without the owner.
        private void OnLoaded(GameLoaded message)
        {
            if (message.SameDay)
            {
                Restore();
            }
            else
            {
                _run.Reset();
            }
        }

        private void OnNewGame(NewGameStarted message)
        {
            _run.Reset();
        }

        private void Restore()
        {
            foreach (var group in Service.Tables)
            {
                if (group == null || _inside.ContainsKey(group))
                {
                    continue;
                }

                if (group.State == GroupState.Leaving)
                {
                    foreach (var member in group.Members)
                    {
                        eventBus.Publish(new CustomerLeft(member.Id));
                    }

                    Service.Remove(group);
                    continue;
                }

                group.SitAll();
                BringSeated(group);
                if (group.State == GroupState.Ordered)
                {
                    ShowOrder(group);
                    var dishIds = new List<string>();
                    foreach (var dish in group.Order)
                    {
                        dishIds.Add(dish.Id);
                    }

                    eventBus.Publish(new OrderTaken(group.Table, dishIds, CustomerIds(group), true));
                }
                else
                {
                    Show(group);
                }
            }
        }

        // Game time only moves forward within a day; the first time heard is just a starting point.
        private void OnTimeChanged(TimeChanged message)
        {
            if (_run.LastMinutes >= 0 && message.Minutes > _run.LastMinutes)
            {
                foreach (var group in Service.Advance(message.Minutes - _run.LastMinutes))
                {
                    Show(group);
                }
            }

            _run.LastMinutes = message.Minutes;
            if (!_run.Closed && message.Minutes >= config.CloseMinute)
            {
                _run.Closed = true;
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
            _queue.Clear();
            _run.Reset();
        }

        private void OnMenuToggled(GameMenuToggled message)
        {
            _paused = message.Open;
            foreach (var customer in Customers)
            {
                customer.SetPaused(_paused);
            }
        }

        // A click on a table's bubble, with the owner close enough: "!" takes its order, a dish serves it.
        // A click on the till, or on a coin bubble, cashes a bill.
        private void OnTargetClicked(TargetClicked message)
        {
            if (_paused || message.Target == null || player == null)
            {
                return;
            }

            if (till != null && message.Target.transform.IsChildOf(till))
            {
                if (Near(till.position, config.TillReach))
                {
                    CashFirst();
                }

                return;
            }

            foreach (var pair in _bubbles)
            {
                var group = pair.Key;
                if (!pair.Value.Contains(message.Target.gameObject))
                {
                    continue;
                }

                if (group.State == GroupState.Paying)
                {
                    if (till == null || Near(till.position, config.TillReach))
                    {
                        Cash(group);
                    }

                    return;
                }

                if (!Near(tables[group.Table].transform.position, config.OrderReach))
                {
                    return;
                }

                if (group.State == GroupState.Ready)
                {
                    TakeOrder(group);
                }
                else if (group.State == GroupState.Ordered)
                {
                    Serve(group);
                }

                return;
            }
        }

        private void Serve(TableGroup group)
        {
            if (ownerInventory == null || !Service.Serve(group, ownerInventory.Inventory, out var payment))
            {
                return;
            }

            eventBus.Publish(new OrderServed(group.Table, group.DishIds(), payment, false, CustomerIds(group)));
            Debug.Log($"[Cafe] Table {group.Table} served, bill {payment}");
            Show(group);
        }

        private bool Cash(TableGroup group)
        {
            if (!Service.Pay(group, out var payment))
            {
                return false;
            }

            _queue.Remove(group);
            HideBubble(group);
            eventBus.Publish(new BillPaid(group.Table, group.DishIds(), payment, group.ServedByEmployee));
            Debug.Log($"[Cafe] Table {group.Table} paid {payment} at the till");
            Show(group);
            LineUp();
            return true;
        }

        private bool Near(Vector3 point, float reach)
        {
            return Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(point.x, point.z)) <= reach;
        }

        // Every customer waiting to pay walks to their place in the queue; the first of each group shows the coin bubble.
        private void LineUp()
        {
            if (tillQueue == null)
            {
                return;
            }

            var place = 0;
            foreach (var group in _queue)
            {
                if (!_inside.TryGetValue(group, out var customers))
                {
                    continue;
                }

                for (var i = 0; i < customers.Count; i++)
                {
                    var customer = customers[i];
                    var first = i == 0;
                    var spot = tillQueue.position - tillQueue.forward * (config.QueueSpacing * place++);
                    customer.WalkTo(spot, () =>
                    {
                        customer.Face(till != null ? till.position : spot + tillQueue.forward);
                        if (first && group.State == GroupState.Paying && !_bubbles.ContainsKey(group))
                        {
                            ShowPayBubble(group, customer);
                        }
                    });
                    customer.SetPaused(_paused);
                }
            }
        }

        // The coin bubble follows the customer as the queue moves up.
        private void ShowPayBubble(TableGroup group, CafeCustomer customer)
        {
            if (config.BubblePrefab == null)
            {
                return;
            }

            var bubble = SpawnBubble(customer.AboveHead(config.OrderBubbleAboveHead), config.PayIcon);
            bubble.transform.SetParent(customer.transform, true);
            _bubbles[group] = new List<GameObject> { bubble };
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

            eventBus.Publish(new OrderTaken(group.Table, dishIds, CustomerIds(group)));
            Debug.Log($"[Cafe] Table {group.Table} ordered: {string.Join(", ", dishIds)}");
        }

        private static List<string> CustomerIds(TableGroup group)
        {
            var ids = new List<string>();
            foreach (var member in group.Members)
            {
                ids.Add(member.Id);
            }

            return ids;
        }

        private void Bring(TableGroup group)
        {
            var seats = tables[group.Table].Seats;
            var customers = new List<CafeCustomer>();
            _inside[group] = customers;
            for (var i = 0; i < group.Members.Count; i++)
            {
                var customer = Spawn(group.Members[i]);
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

        private void BringSeated(TableGroup group)
        {
            var seats = tables[group.Table].Seats;
            var customers = new List<CafeCustomer>();
            _inside[group] = customers;
            for (var i = 0; i < group.Members.Count; i++)
            {
                var customer = Spawn(group.Members[i]);
                customers.Add(customer);
                customer.PlaceSeated(seats[i]);
                customer.SetPaused(_paused);
            }
        }

        private CafeCustomer Spawn(CustomerData member)
        {
            var model = Instantiate(member.Model, door.position, door.rotation, transform);
            model.name = member.Id;
            model.SetActive(true);
            model.AddComponent<NavMeshAgent>();
            var customer = model.AddComponent<CafeCustomer>();
            customer.Init(member, config.WalkSpeed, config.CustomerRadius, config.SitSeconds);
            return customer;
        }

        private void Show(TableGroup group)
        {
            switch (group.State)
            {
                case GroupState.Ready:
                    ShowBubble(group);
                    break;
                case GroupState.Eating:
                    HideBubble(group);
                    break;
                case GroupState.Paying:
                    HideBubble(group);
                    if (!_queue.Contains(group))
                    {
                        _queue.Add(group);
                    }

                    LineUp();
                    break;
                case GroupState.Leaving:
                    if (group.Order.Count > 0 && !group.IsServed)
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

        // Each customer shows their own dish above their head.
        private void ShowOrder(TableGroup group)
        {
            if (config.BubblePrefab == null)
            {
                return;
            }

            var customers = _inside[group];
            var bubbles = new List<GameObject>();
            for (var i = 0; i < group.Order.Count; i++)
            {
                bubbles.Add(SpawnBubble(customers[i].AboveHead(config.OrderBubbleAboveHead), group.Order[i].Icon));
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
