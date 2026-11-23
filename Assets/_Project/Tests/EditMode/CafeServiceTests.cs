using System;
using System.Collections.Generic;
using System.Linq;
using Moonbrew.Cafe;
using Moonbrew.Core;
using Moonbrew.Inventory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Moonbrew.Tests.EditMode
{
    public class CafeServiceTests
    {
        private const int Open = 11 * 60;
        private const int Close = 18 * 60;
        private const int Gap = 30;

        private readonly List<Object> _created = new List<Object>();
        private CustomerData _knight;
        private CustomerData _goblin;
        private CustomerData _wizard;
        private CustomerData _rose;

        [SetUp]
        public void SetUp()
        {
            _knight = Customer("customer_knight", CustomerKind.Passing);
            _goblin = Customer("customer_goblin", CustomerKind.Passing);
            _wizard = Customer("customer_wizard", CustomerKind.Passing);
            _rose = Customer("villager_rose", CustomerKind.Villager);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void TryArrive_FirstGroup_ComesOneGapAfterOpening()
        {
            var service = Service(new[] { 3 }, villagerChance: 0f);

            Assert.IsNull(service.TryArrive(Open - 120), "The first call only sets the first arrival.");
            Assert.IsNull(service.TryArrive(Open + Gap - 1));
            var group = service.TryArrive(Open + Gap);

            Assert.IsNotNull(group);
            Assert.AreSame(group, service.Tables[0]);
            Assert.AreEqual(GroupState.Arriving, group.State);
        }

        [Test]
        public void TryArrive_Closed_NobodyComes()
        {
            var service = Service(new[] { 3, 3 }, villagerChance: 0f);
            service.TryArrive(Close - Gap);

            Assert.IsNull(service.TryArrive(Close));
            Assert.IsNull(service.TryArrive(Close + 200));
        }

        [Test]
        public void TryArrive_NoFreeTable_NobodyComes()
        {
            var service = Service(new[] { 3 }, villagerChance: 0f);
            service.TryArrive(Open);
            service.TryArrive(Open + Gap);

            Assert.IsNull(service.TryArrive(Open + Gap * 2));
        }

        [Test]
        public void TryArrive_NoTableOrNoCustomer_NobodyComes()
        {
            var noTables = new CafeService(Config(0f), new int[0], new[] { _knight }, new Random(1));
            var noCustomers = new CafeService(Config(0f), new[] { 3 }, new CustomerData[0], new Random(1));

            noTables.TryArrive(Open);
            noCustomers.TryArrive(Open);
            Assert.IsNull(noTables.TryArrive(Open + Gap));
            Assert.IsNull(noCustomers.TryArrive(Open + Gap));
        }

        [Test]
        public void TryArrive_Villager_ComesAlone()
        {
            var service = Service(new[] { 3 }, villagerChance: 1f);
            service.TryArrive(Open);

            var group = service.TryArrive(Open + Gap);

            CollectionAssert.AreEqual(new[] { _rose }, group.Members);
        }

        [Test]
        public void TryArrive_PassingGroup_HasDistinctPassersThatFitTheTable()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var service = new CafeService(Config(0f), new[] { 2 }, new[] { _knight, _goblin, _wizard, _rose }, new Random(seed));
                service.TryArrive(Open);
                var group = service.TryArrive(Open + Gap);

                Assert.That(group.Members.Count, Is.InRange(1, 2), "A group fits its table.");
                Assert.AreEqual(group.Members.Count, group.Members.Distinct().Count());
                Assert.IsTrue(group.Members.All(member => member.Kind == CustomerKind.Passing));
            }
        }

        [Test]
        public void TryArrive_CustomerAlreadyInside_DoesNotComeTwice()
        {
            var service = new CafeService(Config(1f), new[] { 1, 1 }, new[] { _rose, _knight }, new Random(3));
            service.TryArrive(Open);
            var first = service.TryArrive(Open + Gap);
            var second = service.TryArrive(Open + Gap * 2);

            CollectionAssert.AreEqual(new[] { _rose }, first.Members);
            CollectionAssert.AreEqual(new[] { _knight }, second.Members, "Rose is inside: a passer comes instead.");
        }

        [Test]
        public void Advance_SeatedGroups_GetReadyThenLeave()
        {
            var service = Service(new[] { 3 }, villagerChance: 1f);
            service.TryArrive(Open);
            var group = service.TryArrive(Open + Gap);
            group.Sit();

            CollectionAssert.AreEqual(new[] { group }, service.Advance(10));
            Assert.AreEqual(GroupState.Ready, group.State);
            CollectionAssert.AreEqual(new[] { group }, service.Advance(60));
            Assert.AreEqual(GroupState.Leaving, group.State);
            Assert.IsEmpty(service.Advance(10));
        }

        [Test]
        public void Close_EveryGroup_LeavesOnce()
        {
            var service = Service(new[] { 1, 1 }, villagerChance: 0f);
            service.TryArrive(Open);
            var first = service.TryArrive(Open + Gap);
            var second = service.TryArrive(Open + Gap * 2);
            first.Leave();

            CollectionAssert.AreEqual(new[] { second }, service.Close());
            Assert.IsEmpty(service.Close());
        }

        [Test]
        public void Remove_LeftGroup_FreesItsTable()
        {
            var service = Service(new[] { 3 }, villagerChance: 0f);
            service.TryArrive(Open);
            var group = service.TryArrive(Open + Gap);

            service.Remove(group);
            service.Remove(null);

            Assert.IsNull(service.Tables[0]);
            Assert.IsNotNull(service.TryArrive(Open + Gap * 2));
        }

        [Test]
        public void Reset_NewDay_EmptiesTheRoomAndWaitsForOpening()
        {
            var service = Service(new[] { 3 }, villagerChance: 0f);
            service.TryArrive(Open);
            service.TryArrive(Open + Gap);

            service.Reset();

            Assert.IsNull(service.Tables[0]);
            Assert.IsNull(service.TryArrive(6 * 60));
            Assert.IsNotNull(service.TryArrive(Open + Gap));
        }

        [Test]
        public void Constructor_NullConfigOrRandom_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CafeService(null, new[] { 1 }, new CustomerData[0], new Random(1)));
            Assert.Throws<ArgumentNullException>(() => new CafeService(Config(0f), new[] { 1 }, new CustomerData[0], null));
        }

        [Test]
        public void TakeOrder_ReadyGroup_GivesOneDishPerMember()
        {
            var cake = Item("dish_cake");
            var service = OrderService(new[] { _knight, _goblin }, cake);
            var group = ReadyGroup(service, 2);

            Assert.IsTrue(service.TakeOrder(group, new[] { new MenuDish("dish_cake", new string[0]) }));

            Assert.AreEqual(GroupState.Ordered, group.State);
            Assert.AreEqual(group.Members.Count, group.Order.Count);
            Assert.IsTrue(group.Order.All(dish => dish == cake));
        }

        [Test]
        public void TakeOrder_LovedDish_IsAlwaysPicked()
        {
            var cake = Item("dish_cake");
            var juice = Item("dish_juice");
            var fan = Customer("villager_fan", CustomerKind.Villager, new[] { juice }, null);
            for (var seed = 0; seed < 20; seed++)
            {
                var service = OrderService(new[] { fan }, new[] { cake, juice }, seed);
                var group = ReadyGroup(service, 1);

                service.TakeOrder(group, Menu("dish_cake", "dish_juice"));

                Assert.AreSame(juice, group.Order[0]);
            }
        }

        [Test]
        public void TakeOrder_LovedIngredient_MakesTheDishLoved()
        {
            var cake = Item("dish_cake");
            var tart = Item("dish_tart");
            var berry = Item("crop_berry");
            var fan = Customer("villager_fan", CustomerKind.Villager, new[] { berry }, null);
            var service = OrderService(new[] { fan }, new[] { cake, tart, berry }, 3);
            var group = ReadyGroup(service, 1);

            service.TakeOrder(group, new[] { new MenuDish("dish_cake", new string[0]), new MenuDish("dish_tart", new[] { "crop_berry" }) });

            Assert.AreSame(tart, group.Order[0]);
        }

        [Test]
        public void TakeOrder_DislikedDish_IsAvoided()
        {
            var cake = Item("dish_cake");
            var juice = Item("dish_juice");
            var picky = Customer("villager_picky", CustomerKind.Villager, null, new[] { cake });
            for (var seed = 0; seed < 20; seed++)
            {
                var service = OrderService(new[] { picky }, new[] { cake, juice }, seed);
                var group = ReadyGroup(service, 1);

                service.TakeOrder(group, Menu("dish_cake", "dish_juice"));

                Assert.AreSame(juice, group.Order[0]);
            }
        }

        [Test]
        public void TakeOrder_OnlyDislikedDishes_StillOrders()
        {
            var cake = Item("dish_cake");
            var picky = Customer("villager_picky", CustomerKind.Villager, null, new[] { cake });
            var service = OrderService(new[] { picky }, cake);
            var group = ReadyGroup(service, 1);

            Assert.IsTrue(service.TakeOrder(group, Menu("dish_cake")));

            Assert.AreSame(cake, group.Order[0]);
        }

        [Test]
        public void TakeOrder_StillChoosing_DoesNothing()
        {
            var cake = Item("dish_cake");
            var service = OrderService(new[] { _knight }, cake);
            service.TryArrive(Open);
            var group = service.TryArrive(Open + Gap);
            group.Sit();

            Assert.IsFalse(service.TakeOrder(group, Menu("dish_cake")));

            Assert.AreEqual(GroupState.Choosing, group.State);
            Assert.IsEmpty(group.Order);
        }

        [Test]
        public void TakeOrder_EmptyNullOrUnknownMenu_ChangesNothing()
        {
            var cake = Item("dish_cake");
            var service = OrderService(new[] { _knight }, cake);
            var group = ReadyGroup(service, 1);

            Assert.IsFalse(service.TakeOrder(group, new MenuDish[0]));
            Assert.IsFalse(service.TakeOrder(group, null));
            Assert.IsFalse(service.TakeOrder(group, Menu("dish_unknown")));
            Assert.IsFalse(service.TakeOrder(null, Menu("dish_cake")));
            Assert.AreEqual(GroupState.Ready, group.State);
        }

        [Test]
        public void Serve_WholeOrderHeld_RemovesItAndPays()
        {
            var cake = PricedItem("dish_cake", 10);
            var service = OrderService(new[] { _knight, _goblin }, cake);
            var group = ReadyGroup(service, 2);
            service.TakeOrder(group, Menu("dish_cake"));
            var inventory = new ItemInventory(10);
            inventory.Add(cake, 3);

            Assert.IsTrue(service.Serve(group, inventory, out var payment));

            Assert.AreEqual(20, payment);
            Assert.AreEqual(1, inventory.Count(cake));
            Assert.AreEqual(GroupState.Leaving, group.State);
            Assert.IsTrue(group.IsServed);
        }

        [Test]
        public void Serve_IncompleteOrder_IsRefusedAndRemovesNothing()
        {
            var cake = PricedItem("dish_cake", 10);
            var service = OrderService(new[] { _knight, _goblin }, cake);
            var group = ReadyGroup(service, 2);
            service.TakeOrder(group, Menu("dish_cake"));
            var inventory = new ItemInventory(10);
            inventory.Add(cake, 1);

            Assert.IsFalse(service.Serve(group, inventory, out var payment));

            Assert.AreEqual(0, payment);
            Assert.AreEqual(1, inventory.Count(cake));
            Assert.AreEqual(GroupState.Ordered, group.State);
        }

        [Test]
        public void Serve_EmptyInventory_IsRefused()
        {
            var cake = PricedItem("dish_cake", 10);
            var service = OrderService(new[] { _knight }, cake);
            var group = ReadyGroup(service, 1);
            service.TakeOrder(group, Menu("dish_cake"));

            Assert.IsFalse(service.Serve(group, new ItemInventory(10), out _));

            Assert.AreEqual(GroupState.Ordered, group.State);
        }

        [Test]
        public void Serve_OrderNotTaken_IsRefused()
        {
            var cake = PricedItem("dish_cake", 10);
            var service = OrderService(new[] { _knight }, cake);
            var group = ReadyGroup(service, 1);
            var inventory = new ItemInventory(10);
            inventory.Add(cake, 1);

            Assert.IsFalse(service.Serve(group, inventory, out _));

            Assert.AreEqual(1, inventory.Count(cake));
            Assert.AreEqual(GroupState.Ready, group.State);
        }

        [Test]
        public void Serve_Twice_TakesAndPaysOnce()
        {
            var cake = PricedItem("dish_cake", 10);
            var service = OrderService(new[] { _knight }, cake);
            var group = ReadyGroup(service, 1);
            service.TakeOrder(group, Menu("dish_cake"));
            var inventory = new ItemInventory(10);
            inventory.Add(cake, 2);
            service.Serve(group, inventory, out _);

            Assert.IsFalse(service.Serve(group, inventory, out var second));

            Assert.AreEqual(0, second);
            Assert.AreEqual(1, inventory.Count(cake));
        }

        [Test]
        public void Serve_NullGroupOrInventory_IsRefused()
        {
            var cake = PricedItem("dish_cake", 10);
            var service = OrderService(new[] { _knight }, cake);
            var group = ReadyGroup(service, 1);
            service.TakeOrder(group, Menu("dish_cake"));

            Assert.IsFalse(service.Serve(null, new ItemInventory(10), out _));
            Assert.IsFalse(service.Serve(group, null, out _));
            Assert.AreEqual(GroupState.Ordered, group.State);
        }

        private ItemData PricedItem(string id, int price)
        {
            var item = Item(id);
            var serialized = new SerializedObject(item);
            serialized.FindProperty("basePrice").intValue = price;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static MenuDish[] Menu(params string[] dishIds)
        {
            return dishIds.Select(id => new MenuDish(id, new string[0])).ToArray();
        }

        private CafeService OrderService(CustomerData[] customers, ItemData item)
        {
            return OrderService(customers, new[] { item }, 7);
        }

        private CafeService OrderService(CustomerData[] customers, ItemData[] items, int seed)
        {
            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            catalog.Init(items);
            _created.Add(catalog);
            var config = ScriptableObject.CreateInstance<CafeConfig>();
            config.Init(null, 11, 18, Gap, Gap, customers.Length, 0f, 10, 10, 60);
            config.InitOrders(catalog, 2.5f);
            _created.Add(config);
            return new CafeService(config, new[] { customers.Length }, customers, new Random(seed));
        }

        // Arrivals pick a random group size: try until the whole party came in.
        private static TableGroup ReadyGroup(CafeService service, int size)
        {
            var minute = Open;
            service.TryArrive(minute);
            TableGroup group = null;
            while (group == null || group.Members.Count != size)
            {
                service.Reset();
                service.TryArrive(minute);
                group = service.TryArrive(minute + Gap);
            }

            for (var i = 0; i < size; i++)
            {
                group.Sit();
            }

            group.Advance(10);
            return group;
        }

        private ItemData Item(string id)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            var serialized = new SerializedObject(item);
            serialized.FindProperty("id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _created.Add(item);
            return item;
        }

        private CustomerData Customer(string id, CustomerKind kind, ItemData[] loved, ItemData[] disliked)
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            customer.Init(id, kind, loved, disliked);
            _created.Add(customer);
            return customer;
        }

        private CafeService Service(int[] tables, float villagerChance)
        {
            return new CafeService(Config(villagerChance), tables, new[] { _knight, _goblin, _wizard, _rose }, new Random(7));
        }

        private CafeConfig Config(float villagerChance)
        {
            var config = ScriptableObject.CreateInstance<CafeConfig>();
            config.Init(null, 11, 18, Gap, Gap, 3, villagerChance, 10, 10, 60);
            _created.Add(config);
            return config;
        }

        private CustomerData Customer(string id, CustomerKind kind)
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            customer.Init(id, kind, null, null);
            _created.Add(customer);
            return customer;
        }
    }
}
