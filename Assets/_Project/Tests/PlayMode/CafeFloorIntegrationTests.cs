using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Moonbrew.Cafe;
using Moonbrew.Core;
using Moonbrew.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.PlayMode
{
    // Customers come in through the door, walk to a free table on the NavMesh, sit, show the "!" bubble
    // once they have chosen, and leave when out of patience or at closing time.
    public class CafeFloorIntegrationTests
    {
        private const int Open = 11 * 60;
        private const int Close = 18 * 60;
        private const int Gap = 30;
        private const int Choose = 10;
        private const int Patience = 60;
        private const float TimeoutSeconds = 10f;
        private const float Reach = 2.5f;
        private const string DishId = "dish_cake";
        private const int DishPrice = 10;
        private const int Eat = 20;

        private readonly List<Object> _created = new List<Object>();
        private readonly List<string> _left = new List<string>();
        private readonly List<string> _taken = new List<string>();
        private readonly List<int> _abandoned = new List<int>();
        private readonly List<OrderServed> _served = new List<OrderServed>();
        private readonly List<BillPaid> _paid = new List<BillPaid>();
        private ClickTarget _till;
        private InventoryHolder _ownerItems;
        private ItemData _dish;
        private Transform _owner;
        private NavMeshDataInstance _navMesh;
        private EventBusAsset _bus;
        private CafeFloor _floor;
        private CafeConfig _floorConfig;
        private CafeTable _table;
        private Transform _door;

        [SetUp]
        public void SetUp()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _created.Add(ground);
            var bounds = new Bounds(Vector3.zero, new Vector3(20f, 4f, 20f));
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            var data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0), sources, bounds, Vector3.zero, Quaternion.identity);
            _navMesh = NavMesh.AddNavMeshData(data);

            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _bus.Subscribe<CustomerLeft>(message => _left.Add(message.CustomerId));

            var body = Create(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            Object.Destroy(body.GetComponent<Collider>());
            body.SetActive(false);
            var knight = Customer("customer_knight", body);
            var goblin = Customer("customer_goblin", body);
            var catalog = Create(ScriptableObject.CreateInstance<CustomerCatalog>());
            catalog.Init(new[] { knight, goblin });

            var bubble = Create(new GameObject("Bubble"));
            new GameObject("Icon").AddComponent<SpriteRenderer>().transform.SetParent(bubble.transform);
            bubble.AddComponent<ClickTarget>();
            bubble.SetActive(false);
            var items = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            _dish = Dish(DishId);
            items.Init(new[] { _dish });
            var config = Create(ScriptableObject.CreateInstance<CafeConfig>());
            config.Init(catalog, 11, 18, Gap, Gap, 2, 0f, Choose, Choose, Patience);
            config.InitVisuals(6f, bubble, null);
            config.InitOrders(items, Reach);
            config.InitTill(Eat, null, Reach);
            _bus.Subscribe<MenuRequested>(message => message.Menu.Add(new MenuDish(DishId, new string[0])));
            _bus.Subscribe<OrderTaken>(message => _taken.AddRange(message.DishIds));
            _bus.Subscribe<OrderAbandoned>(message => _abandoned.Add(message.Table));
            _bus.Subscribe<OrderServed>(message => _served.Add(message));
            _bus.Subscribe<BillPaid>(message => _paid.Add(message));
            _owner = Create(new GameObject("Owner")).transform;
            _owner.position = new Vector3(3f, 0f, 1f);
            var holderObject = Create(new GameObject("Owner Items"));
            holderObject.SetActive(false);
            _ownerItems = holderObject.AddComponent<InventoryHolder>();
            _ownerItems.Init(Create(ScriptableObject.CreateInstance<InventoryConfig>()), items, _bus);
            holderObject.SetActive(true);

            var table = Create(new GameObject("Table"));
            table.transform.position = new Vector3(3f, 0f, 0f);
            table.AddComponent<CafeTable>().Init(new[] { Seat(table, 2.5f, -0.6f), Seat(table, 2.5f, 0.6f) });
            var door = Create(new GameObject("Door")).transform;
            door.position = new Vector3(-4f, 0f, 0f);

            var floorObject = Create(new GameObject("Cafe Floor"));
            floorObject.SetActive(false);
            _floor = floorObject.AddComponent<CafeFloor>();
            _floorConfig = config;
            _table = table.GetComponent<CafeTable>();
            _door = door;
            _floor.Init(config, _bus, new[] { _table }, door, new System.Random(4), _owner, _ownerItems);
            _till = Create(new GameObject("Till")).AddComponent<ClickTarget>();
            _till.transform.position = new Vector3(2f, 0.5f, 2.5f);
            var queue = Create(new GameObject("Till Queue")).transform;
            queue.SetPositionAndRotation(new Vector3(2f, 0f, 1.5f), Quaternion.LookRotation(Vector3.forward));
            _floor.InitTill(_till.transform, queue);
            floorObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }

            _created.Clear();
            _left.Clear();
            _taken.Clear();
            _abandoned.Clear();
            _served.Clear();
            _paid.Clear();
            NavMesh.RemoveNavMeshData(_navMesh);
        }

        [UnityTest]
        public IEnumerator Group_ComesSitsChoosesThenLeavesOutOfPatience()
        {
            var group = Arrive();
            Assert.IsNotNull(group, "A group should come in once the first gap has passed.");
            yield return WaitUntil(() => _floor.Customers.All(customer => customer.IsSeated));

            Assert.AreEqual(GroupState.Choosing, group.State);
            Assert.IsFalse(_floor.HasBubble(0), "No bubble while choosing.");
            foreach (var customer in _floor.Customers)
            {
                Assert.Less(Vector3.Distance(customer.transform.position, new Vector3(3f, 0f, 0f)), 1f, "Seated at the table.");
                Assert.IsFalse(customer.GetComponent<NavMeshAgent>().enabled);
                Assert.IsTrue(customer.GetComponent<NavMeshObstacle>().enabled, "Walking customers go around a seated one.");
            }

            _bus.Publish(new TimeChanged(Open + Gap + Choose));
            Assert.AreEqual(GroupState.Ready, group.State);
            Assert.IsTrue(_floor.HasBubble(0), "The bubble shows once they have chosen.");

            _bus.Publish(new TimeChanged(Open + Gap + Choose + Patience));
            Assert.AreEqual(GroupState.Leaving, group.State);
            Assert.IsFalse(_floor.HasBubble(0));
            yield return WaitUntil(() => _left.Count == group.Members.Count);

            CollectionAssert.AreEquivalent(group.Members.Select(member => member.Id), _left);
            Assert.IsNull(_floor.Service.Tables[0], "The table is free again.");
            Assert.IsEmpty(_floor.Customers);
        }

        [UnityTest]
        public IEnumerator ClosingTime_SeatedGroup_Leaves()
        {
            var group = Arrive();
            yield return WaitUntil(() => _floor.Customers.All(customer => customer.IsSeated));

            _bus.Publish(new TimeChanged(Close));

            Assert.AreEqual(GroupState.Leaving, group.State);
            yield return WaitUntil(() => _left.Count == group.Members.Count);
            Assert.IsNull(_floor.Service.Tables[0]);
        }

        [UnityTest]
        public IEnumerator Night_EmptiesTheCafe()
        {
            Arrive();
            yield return null;

            _bus.Publish(new NightPassed(2));
            yield return null;

            Assert.IsEmpty(_floor.Customers);
            Assert.IsNull(_floor.Service.Tables[0]);
            Assert.IsEmpty(_left, "Nobody walked out: the café was emptied for the night.");
        }

        [UnityTest]
        public IEnumerator ClickBubble_OwnerNearby_TakesTheWholeTableOrder()
        {
            var group = Arrive();
            yield return WaitUntil(() => _floor.Customers.All(customer => customer.IsSeated));
            _bus.Publish(new TimeChanged(Open + Gap + Choose));

            ClickBubble();

            Assert.AreEqual(GroupState.Ordered, group.State);
            Assert.AreEqual(group.Members.Count, _taken.Count, "One dish per customer.");
            Assert.AreEqual(group.Members.Count, _floor.BubbleCount(0), "One bubble per customer.");
            var customers = _floor.Customers.ToList();
            for (var i = 0; i < customers.Count; i++)
            {
                var bubble = _floor.Bubbles(0)[i].transform.position;
                var customer = customers[i].transform.position;
                Assert.AreEqual(customer.x, bubble.x, 0.01f, "The bubble is right above its customer.");
                Assert.AreEqual(customer.z, bubble.z, 0.01f, "The bubble is right above its customer.");
                Assert.Greater(bubble.y, customer.y);
            }
        }

        [UnityTest]
        public IEnumerator ClickBubble_OwnerTooFar_DoesNothing()
        {
            var group = Arrive();
            yield return WaitUntil(() => _floor.Customers.All(customer => customer.IsSeated));
            _bus.Publish(new TimeChanged(Open + Gap + Choose));
            _owner.position = new Vector3(-8f, 0f, 0f);

            ClickBubble();

            Assert.AreEqual(GroupState.Ready, group.State);
            Assert.IsEmpty(_taken);
        }

        [UnityTest]
        public IEnumerator ClickBubble_MenuOpen_DoesNothing()
        {
            var group = Arrive();
            yield return WaitUntil(() => _floor.Customers.All(customer => customer.IsSeated));
            _bus.Publish(new TimeChanged(Open + Gap + Choose));
            _bus.Publish(new GameMenuToggled(true));

            ClickBubble();

            Assert.AreEqual(GroupState.Ready, group.State);
        }

        [UnityTest]
        public IEnumerator Ordered_OutOfPatience_AbandonsTheOrder()
        {
            var group = Arrive();
            yield return WaitUntil(() => _floor.Customers.All(customer => customer.IsSeated));
            _bus.Publish(new TimeChanged(Open + Gap + Choose));
            ClickBubble();

            _bus.Publish(new TimeChanged(Open + Gap + Choose + Patience));

            Assert.AreEqual(GroupState.Leaving, group.State);
            CollectionAssert.AreEqual(new[] { 0 }, _abandoned);
            Assert.IsFalse(_floor.HasBubble(0));
        }

        [UnityTest]
        public IEnumerator Ordered_Night_AbandonsTheOrder()
        {
            Arrive();
            yield return WaitUntil(() => _floor.Customers.All(customer => customer.IsSeated));
            _bus.Publish(new TimeChanged(Open + Gap + Choose));
            ClickBubble();

            _bus.Publish(new NightPassed(2));

            CollectionAssert.AreEqual(new[] { 0 }, _abandoned);
        }

        [UnityTest]
        public IEnumerator ClickDish_WholeOrderHeld_ServesAndTheTableEats()
        {
            var group = Arrive();
            yield return WaitUntil(() => _floor.Customers.All(customer => customer.IsSeated));
            _bus.Publish(new TimeChanged(Open + Gap + Choose));
            ClickBubble();
            _ownerItems.Inventory.Add(_dish, group.Members.Count + 1);

            ClickBubble();

            Assert.AreEqual(GroupState.Eating, group.State);
            Assert.AreEqual(1, _ownerItems.Inventory.Count(_dish));
            Assert.AreEqual(1, _served.Count);
            Assert.AreEqual(0, _served[0].Table);
            Assert.AreEqual(group.Members.Count, _served[0].DishIds.Count);
            Assert.AreEqual(group.Members.Count * DishPrice, _served[0].Payment);
            Assert.IsEmpty(_paid, "The bill is paid at the till, not at the table.");
            Assert.IsEmpty(_abandoned, "A served table has not abandoned its order.");
            Assert.IsFalse(_floor.HasBubble(0));
            Assert.IsTrue(_floor.Customers.All(customer => customer.IsSeated), "They eat seated.");
        }

        [UnityTest]
        public IEnumerator DoneEating_QueueAtTheTill_TheOwnerCashesThenTheyLeave()
        {
            var served = Arrive();
            yield return Served(served);
            _bus.Publish(new TimeChanged(Open + Gap + Choose + Eat));

            Assert.AreEqual(GroupState.Paying, served.State);
            CollectionAssert.AreEqual(new[] { served }, _floor.Queue);
            yield return WaitUntil(() => _floor.HasBubble(0));
            Assert.Less(Vector3.Distance(_floor.Customers.First().transform.position, new Vector3(2f, 0f, 1.5f)), 1f, "The first one stands at the till.");

            _bus.Publish(new TargetClicked(_till));

            Assert.AreEqual(1, _paid.Count);
            Assert.AreEqual(served.Members.Count * DishPrice, _paid[0].Payment);
            Assert.IsFalse(_paid[0].ByEmployee);
            Assert.AreEqual(GroupState.Leaving, served.State);
            Assert.IsEmpty(_floor.Queue);
            Assert.IsFalse(_floor.HasBubble(0));
            yield return WaitUntil(() => _left.Count == served.Members.Count);
            Assert.IsNull(_floor.Service.Tables[0], "The table is free again.");
        }

        [UnityTest]
        public IEnumerator ClickTill_OwnerTooFarOrNobodyWaiting_CashesNothing()
        {
            _bus.Publish(new TargetClicked(_till));
            Assert.IsFalse(_floor.CashFirst());
            var served = Arrive();
            yield return Served(served);
            _bus.Publish(new TimeChanged(Open + Gap + Choose + Eat));
            _owner.position = new Vector3(9f, 0f, 9f);

            _bus.Publish(new TargetClicked(_till));

            Assert.IsEmpty(_paid);
            Assert.AreEqual(GroupState.Paying, served.State);
        }

        [UnityTest]
        public IEnumerator ClosingTime_ServedGroup_StaysToPay()
        {
            var served = Arrive();
            yield return Served(served);

            _bus.Publish(new TimeChanged(Close));

            Assert.AreEqual(GroupState.Paying, served.State, "Done eating by closing time: waiting at the till, not gone.");
            CollectionAssert.AreEqual(new[] { served }, _floor.Queue);
            Assert.IsEmpty(_abandoned);
        }

        // Seated, ordered and served by the owner.
        private IEnumerator Served(TableGroup group)
        {
            yield return WaitUntil(() => _floor.Customers.All(customer => customer.IsSeated));
            _bus.Publish(new TimeChanged(Open + Gap + Choose));
            ClickBubble();
            _ownerItems.Inventory.Add(_dish, group.Members.Count);
            ClickBubble();
            Assert.AreEqual(GroupState.Eating, group.State);
        }

        [UnityTest]
        public IEnumerator ClickDish_IncompleteOrder_IsRefused()
        {
            var group = Arrive();
            yield return WaitUntil(() => _floor.Customers.All(customer => customer.IsSeated));
            _bus.Publish(new TimeChanged(Open + Gap + Choose));
            ClickBubble();
            var held = group.Members.Count - 1;
            if (held > 0)
            {
                _ownerItems.Inventory.Add(_dish, held);
            }

            ClickBubble();

            Assert.AreEqual(GroupState.Ordered, group.State);
            Assert.AreEqual(held, _ownerItems.Inventory.Count(_dish));
            Assert.IsEmpty(_served);
            Assert.IsTrue(_floor.HasBubble(0));
        }

        [UnityTest]
        public IEnumerator ClickDish_OwnerTooFar_DoesNothing()
        {
            var group = Arrive();
            yield return WaitUntil(() => _floor.Customers.All(customer => customer.IsSeated));
            _bus.Publish(new TimeChanged(Open + Gap + Choose));
            ClickBubble();
            _ownerItems.Inventory.Add(_dish, group.Members.Count);
            _owner.position = new Vector3(-8f, 0f, 0f);

            ClickBubble();

            Assert.AreEqual(GroupState.Ordered, group.State);
            Assert.AreEqual(group.Members.Count, _ownerItems.Inventory.Count(_dish));
            Assert.IsEmpty(_served);
        }

        [UnityTest]
        public IEnumerator SameDayLoaded_GroupsFromAway_AreSeatedInTheirState()
        {
            var day = Create(ScriptableObject.CreateInstance<CafeDay>());
            var floor = FloorSharing(day, out var run);
            run.AdvanceAway(Open);
            run.AdvanceAway(Open + Gap + Choose);
            var group = run.Service.Tables[0];

            _bus.Publish(new GameLoaded(SaveData.CreateNew("slot_a", 1), true));
            yield return null;

            Assert.AreEqual(GroupState.Ready, group.State);
            Assert.AreEqual(group.Members.Count, floor.Customers.Count());
            Assert.IsTrue(floor.Customers.All(customer => customer.IsSeated));
            Assert.IsTrue(floor.HasBubble(0));
        }

        [UnityTest]
        public IEnumerator SameDayLoaded_OrderedGroup_ShowsItsOrderAgain()
        {
            var day = Create(ScriptableObject.CreateInstance<CafeDay>());
            var floor = FloorSharing(day, out var run);
            run.AdvanceAway(Open);
            run.AdvanceAway(Open + Gap + Choose);
            var group = run.Service.Tables[0];
            run.Service.TakeOrder(group, new[] { new MenuDish(DishId, new string[0]) });

            _bus.Publish(new GameLoaded(SaveData.CreateNew("slot_a", 1), true));
            yield return null;

            Assert.AreEqual(group.Members.Count, floor.BubbleCount(0));
            Assert.AreEqual(group.Members.Count, _taken.Count, "The order reminder comes back.");
        }

        [UnityTest]
        public IEnumerator SameDayLoaded_LeavingGroup_IsGone()
        {
            var day = Create(ScriptableObject.CreateInstance<CafeDay>());
            var floor = FloorSharing(day, out var run);
            run.AdvanceAway(Open);
            run.AdvanceAway(Open + Gap);
            var group = run.Service.Tables[0];
            group.Leave();

            _bus.Publish(new GameLoaded(SaveData.CreateNew("slot_a", 1), true));
            yield return null;

            Assert.IsEmpty(floor.Customers);
            Assert.IsNull(run.Service.Tables[0]);
            Assert.AreEqual(group.Members.Count, _left.Count);
        }

        [UnityTest]
        public IEnumerator GameLoadedFromSave_EmptiesTheCafeDay()
        {
            var day = Create(ScriptableObject.CreateInstance<CafeDay>());
            var floor = FloorSharing(day, out var run);
            run.AdvanceAway(Open);
            run.AdvanceAway(Open + Gap);

            _bus.Publish(new GameLoaded(SaveData.CreateNew("slot_a", 1)));
            yield return null;

            Assert.IsNull(run.Service.Tables[0]);
            Assert.IsEmpty(floor.Customers);
        }

        [UnityTest]
        public IEnumerator CafeAway_TimePasses_GroupIsWaitingOnReturn()
        {
            var day = Create(ScriptableObject.CreateInstance<CafeDay>());
            _floor.gameObject.SetActive(false);
            var config = _floorConfig;
            config.InitTables(new[] { 2 });
            var awayObject = Create(new GameObject("Cafe Away"));
            awayObject.SetActive(false);
            awayObject.AddComponent<CafeAway>().Init(config, day, _bus);
            awayObject.SetActive(true);

            _bus.Publish(new TimeChanged(Open));
            _bus.Publish(new TimeChanged(Open + Gap + Choose));
            yield return null;

            Assert.AreEqual(GroupState.Ready, day.Run(config, config.TableSeats).Service.Tables[0].State);
        }

        private CafeFloor FloorSharing(CafeDay day, out CafeRun run)
        {
            _floor.gameObject.SetActive(false);
            var floorObject = Create(new GameObject("Shared Floor"));
            floorObject.SetActive(false);
            var floor = floorObject.AddComponent<CafeFloor>();
            floor.Init(_floorConfig, _bus, new[] { _table }, _door, new System.Random(4), _owner, _ownerItems, day);
            run = day.Run(_floorConfig, new[] { _table.Seats.Count });
            floorObject.SetActive(true);
            return floor;
        }

        private void ClickBubble()
        {
            _bus.Publish(new TargetClicked(_floor.Bubbles(0)[0].GetComponent<ClickTarget>()));
        }

        private ItemData Dish(string id)
        {
            var item = Create(ScriptableObject.CreateInstance<ItemData>());
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(item);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("basePrice").intValue = DishPrice;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
            return item;
        }

        private TableGroup Arrive()
        {
            _bus.Publish(new TimeChanged(Open));
            _bus.Publish(new TimeChanged(Open + Gap));
            return _floor.Service.Tables[0];
        }

        private IEnumerator WaitUntil(Func<bool> condition)
        {
            var end = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end)
                {
                    Assert.Fail("Timed out. Customers: " + string.Join("; ", _floor.Customers.Select(Describe)));
                }

                yield return null;
            }
        }

        private static string Describe(CafeCustomer customer)
        {
            var agent = customer.GetComponent<NavMeshAgent>();
            return $"{customer.name} at {customer.transform.position} seated={customer.IsSeated} walking={customer.IsWalking} "
                + $"onMesh={agent.isOnNavMesh} pending={agent.pathPending} status={agent.pathStatus} left={agent.remainingDistance} speed={agent.velocity.magnitude} to={agent.destination} corners={agent.path.corners.Length} stopped={agent.isStopped}";
        }

        private Transform Seat(GameObject table, float x, float z)
        {
            var seat = new GameObject("Seat").transform;
            seat.SetParent(table.transform);
            seat.position = new Vector3(x, 0f, z);
            seat.LookAt(table.transform.position);
            return seat;
        }

        private CustomerData Customer(string id, GameObject model)
        {
            var customer = Create(ScriptableObject.CreateInstance<CustomerData>());
            customer.Init(id, CustomerKind.Passing, null, null, new[] { "Bonjour" }, model);
            return customer;
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
