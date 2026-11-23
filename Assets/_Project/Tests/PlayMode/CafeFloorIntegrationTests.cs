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

        private readonly List<Object> _created = new List<Object>();
        private readonly List<string> _left = new List<string>();
        private readonly List<string> _taken = new List<string>();
        private readonly List<int> _abandoned = new List<int>();
        private Transform _owner;
        private NavMeshDataInstance _navMesh;
        private EventBusAsset _bus;
        private CafeFloor _floor;

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
            items.Init(new[] { Dish(DishId) });
            var config = Create(ScriptableObject.CreateInstance<CafeConfig>());
            config.Init(catalog, 11, 18, Gap, Gap, 2, 0f, Choose, Choose, Patience);
            config.InitVisuals(6f, bubble, null);
            config.InitOrders(items, Reach);
            _bus.Subscribe<MenuRequested>(message => message.Menu.Add(new MenuDish(DishId, new string[0])));
            _bus.Subscribe<OrderTaken>(message => _taken.AddRange(message.DishIds));
            _bus.Subscribe<OrderAbandoned>(message => _abandoned.Add(message.Table));
            _owner = Create(new GameObject("Owner")).transform;
            _owner.position = new Vector3(3f, 0f, 1f);

            var table = Create(new GameObject("Table"));
            table.transform.position = new Vector3(3f, 0f, 0f);
            table.AddComponent<CafeTable>().Init(new[] { Seat(table, 2.5f, -0.6f), Seat(table, 2.5f, 0.6f) });
            var door = Create(new GameObject("Door")).transform;
            door.position = new Vector3(-4f, 0f, 0f);

            var floorObject = Create(new GameObject("Cafe Floor"));
            floorObject.SetActive(false);
            _floor = floorObject.AddComponent<CafeFloor>();
            _floor.Init(config, _bus, new[] { table.GetComponent<CafeTable>() }, door, new System.Random(4), _owner);
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
            Assert.AreEqual(group.Members.Count, _floor.BubbleCount(0), "One bubble per dish.");
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
