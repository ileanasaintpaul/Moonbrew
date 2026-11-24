using System.Collections.Generic;
using Moonbrew.Cafe;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Moonbrew.Tests.EditMode
{
    public class CafeRunTests
    {
        private const int Open = 11 * 60;
        private const int Close = 18 * 60;
        private const int Gap = 30;
        private const int Choose = 10;
        private const int Patience = 60;

        private readonly List<Object> _created = new List<Object>();
        private CafeRun _run;

        [SetUp]
        public void SetUp()
        {
            var customer = Create(ScriptableObject.CreateInstance<CustomerData>());
            customer.Init("customer_knight", CustomerKind.Passing, null, null, new[] { "Hello" }, null);
            var catalog = Create(ScriptableObject.CreateInstance<CustomerCatalog>());
            catalog.Init(new[] { customer });
            var config = Create(ScriptableObject.CreateInstance<CafeConfig>());
            config.Init(catalog, 11, 18, Gap, Gap, 1, 0f, Choose, Choose, Patience);
            _run = new CafeRun(config, new[] { 2 }, new Random(1));
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
        public void AdvanceAway_ArrivalTime_SeatsTheGroup()
        {
            _run.AdvanceAway(Open);

            _run.AdvanceAway(Open + Gap);

            Assert.AreEqual(GroupState.Choosing, _run.Service.Tables[0].State);
        }

        [Test]
        public void AdvanceAway_ChoiceTimePassed_GroupIsReady()
        {
            _run.AdvanceAway(Open);

            _run.AdvanceAway(Open + Gap + Choose);

            Assert.AreEqual(GroupState.Ready, _run.Service.Tables[0].State);
        }

        [Test]
        public void AdvanceAway_OutOfPatience_GroupHasLeft()
        {
            _run.AdvanceAway(Open);
            _run.AdvanceAway(Open + Gap + Choose);
            var group = _run.Service.Tables[0];

            var gone = _run.AdvanceAway(Open + Gap + Choose + Patience);

            CollectionAssert.Contains(gone, group);
            Assert.AreNotSame(group, _run.Service.Tables[0]);
        }

        [Test]
        public void AdvanceAway_FirstTimeOfTheDay_BringsNobody()
        {
            Assert.IsEmpty(_run.AdvanceAway(Open));

            Assert.IsNull(_run.Service.Tables[0]);
            Assert.AreEqual(Open, _run.LastMinutes);
        }

        [Test]
        public void AdvanceAway_SameMinute_ChangesNothing()
        {
            _run.AdvanceAway(Open);
            _run.AdvanceAway(Open + Gap);
            var group = _run.Service.Tables[0];

            Assert.IsEmpty(_run.AdvanceAway(Open + Gap));

            Assert.AreSame(group, _run.Service.Tables[0]);
            Assert.AreEqual(GroupState.Choosing, group.State);
        }

        [Test]
        public void AdvanceAway_ClosingTime_EmptiesTheCafe()
        {
            _run.AdvanceAway(Close - 5);
            _run.AdvanceAway(Close - 4);

            _run.AdvanceAway(Close);

            Assert.IsTrue(_run.Closed);
            Assert.IsNull(_run.Service.Tables[0]);
        }

        [Test]
        public void AdvanceAway_DoneEating_TheEmployeeCashesTheBill()
        {
            var dish = Create(ScriptableObject.CreateInstance<Moonbrew.Inventory.ItemData>());
            _run.AdvanceAway(Open);
            _run.AdvanceAway(Open + Gap + Choose);
            var group = _run.Service.Tables[0];
            group.TakeOrder(new[] { dish });
            _run.Service.ServePrepared(group, out _);
            var paid = new List<TableGroup>();

            var gone = _run.AdvanceAway(Open + Gap + Choose + 20, paid);

            CollectionAssert.AreEqual(new[] { group }, paid);
            CollectionAssert.Contains(gone, group);
            Assert.IsTrue(group.IsPaid);
        }

        [Test]
        public void AdvanceAway_StillEating_NothingIsCashed()
        {
            var dish = Create(ScriptableObject.CreateInstance<Moonbrew.Inventory.ItemData>());
            _run.AdvanceAway(Open);
            _run.AdvanceAway(Open + Gap + Choose);
            var group = _run.Service.Tables[0];
            group.TakeOrder(new[] { dish });
            _run.Service.ServePrepared(group, out _);
            var paid = new List<TableGroup>();

            _run.AdvanceAway(Open + Gap + Choose + 5, paid);

            Assert.IsEmpty(paid);
            Assert.AreEqual(GroupState.Eating, group.State);
        }

        [Test]
        public void AdvanceAway_GroupAlreadyLeaving_IsRemoved()
        {
            _run.AdvanceAway(Open);
            _run.AdvanceAway(Open + Gap);
            var group = _run.Service.Tables[0];
            group.Leave();

            var gone = _run.AdvanceAway(Open + Gap);

            CollectionAssert.AreEqual(new[] { group }, gone);
            Assert.IsNull(_run.Service.Tables[0]);
        }

        [Test]
        public void AdvanceAway_OnlyVillagers_NobodyComes()
        {
            var villager = Create(ScriptableObject.CreateInstance<CustomerData>());
            villager.Init("villager_rose", CustomerKind.Villager, null, null, new[] { "Hello" }, null);
            var catalog = Create(ScriptableObject.CreateInstance<CustomerCatalog>());
            catalog.Init(new[] { villager });
            var config = Create(ScriptableObject.CreateInstance<CafeConfig>());
            config.Init(catalog, 11, 18, Gap, Gap, 1, 1f, Choose, Choose, Patience);
            var run = new CafeRun(config, new[] { 2 }, new Random(1));

            run.AdvanceAway(Open);
            run.AdvanceAway(Open + Gap);

            Assert.IsNull(run.Service.Tables[0]);
        }

        [Test]
        public void Reset_DuringTheDay_EmptiesAndForgetsTheTime()
        {
            _run.AdvanceAway(Open);
            _run.AdvanceAway(Close);

            _run.Reset();

            Assert.IsNull(_run.Service.Tables[0]);
            Assert.AreEqual(-1, _run.LastMinutes);
            Assert.IsFalse(_run.Closed);
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
