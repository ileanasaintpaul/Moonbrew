using System;
using System.Collections.Generic;
using System.Linq;
using Moonbrew.Cafe;
using NUnit.Framework;
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
