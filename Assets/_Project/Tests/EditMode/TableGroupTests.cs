using System;
using Moonbrew.Cafe;
using Moonbrew.Inventory;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.EditMode
{
    public class TableGroupTests
    {
        [Test]
        public void SitAll_ArrivingGroup_StartsChoosing()
        {
            var customers = new[] { ScriptableObject.CreateInstance<CustomerData>(), ScriptableObject.CreateInstance<CustomerData>() };
            var group = new TableGroup(customers, 0, 5, 5);

            group.SitAll();

            Assert.AreEqual(GroupState.Choosing, group.State);
            Object.DestroyImmediate(customers[0]);
            Object.DestroyImmediate(customers[1]);
        }

        [Test]
        public void SitAll_LeavingGroup_StaysLeaving()
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            var group = new TableGroup(new[] { customer }, 0, 5, 5);
            group.Leave();

            group.SitAll();

            Assert.AreEqual(GroupState.Leaving, group.State);
            Object.DestroyImmediate(customer);
        }

        private CustomerData _a;
        private CustomerData _b;

        [SetUp]
        public void SetUp()
        {
            _a = ScriptableObject.CreateInstance<CustomerData>();
            _b = ScriptableObject.CreateInstance<CustomerData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_a);
            Object.DestroyImmediate(_b);
        }

        [Test]
        public void Sit_EveryMember_StartsChoosing()
        {
            var group = new TableGroup(new[] { _a, _b }, 0, 10, 60);

            Assert.IsFalse(group.Sit());
            Assert.AreEqual(GroupState.Arriving, group.State, "The group waits for everyone.");
            Assert.IsTrue(group.Sit());
            Assert.AreEqual(GroupState.Choosing, group.State);
        }

        [Test]
        public void Sit_AlreadySeated_IsIgnored()
        {
            var group = new TableGroup(new[] { _a }, 0, 10, 60);
            group.Sit();

            Assert.IsFalse(group.Sit());
            Assert.AreEqual(GroupState.Choosing, group.State);
        }

        [Test]
        public void Advance_ChoosingTimeOver_IsReadyToOrder()
        {
            var group = Seated(10, 60);

            Assert.IsFalse(group.Advance(9));
            Assert.IsTrue(group.Advance(1));
            Assert.AreEqual(GroupState.Ready, group.State);
        }

        [Test]
        public void Advance_PatienceOver_Leaves()
        {
            var group = Seated(10, 60);
            group.Advance(10);

            Assert.IsFalse(group.Advance(59));
            Assert.IsTrue(group.Advance(5));
            Assert.AreEqual(GroupState.Leaving, group.State);
        }

        [Test]
        public void Advance_ZeroNegativeOrWhileArriving_DoesNothing()
        {
            var arriving = new TableGroup(new[] { _a }, 0, 10, 60);
            var seated = Seated(10, 60);

            Assert.IsFalse(arriving.Advance(100));
            Assert.AreEqual(GroupState.Arriving, arriving.State);
            Assert.IsFalse(seated.Advance(0));
            Assert.IsFalse(seated.Advance(-20));
            Assert.AreEqual(GroupState.Choosing, seated.State);
        }

        [Test]
        public void Advance_ZeroChoosingTime_IsReadyAtTheFirstMinute()
        {
            var group = Seated(0, 60);

            Assert.IsTrue(group.Advance(1));
            Assert.AreEqual(GroupState.Ready, group.State);
        }

        [Test]
        public void Leave_AnyStateButLeaving_GoesAndOnlyOnce()
        {
            var arriving = new TableGroup(new[] { _a }, 0, 10, 60);

            Assert.IsTrue(arriving.Leave());
            Assert.AreEqual(GroupState.Leaving, arriving.State);
            Assert.IsFalse(arriving.Leave());
            Assert.IsFalse(arriving.Sit(), "A leaving group does not sit down again.");
        }

        [Test]
        public void Constructor_NullOrEmptyMembers_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TableGroup(null, 0, 10, 60));
            Assert.Throws<ArgumentException>(() => new TableGroup(new CustomerData[0], 0, 10, 60));
        }

        [Test]
        public void TakeOrder_ReadyGroup_KeepsTheDishesAndWaits()
        {
            var dish = ScriptableObject.CreateInstance<ItemData>();
            var group = Seated(10, 60);
            group.Advance(10);

            Assert.IsTrue(group.TakeOrder(new[] { dish }));

            Assert.AreEqual(GroupState.Ordered, group.State);
            CollectionAssert.AreEqual(new[] { dish }, group.Order);
            Object.DestroyImmediate(dish);
        }

        [Test]
        public void TakeOrder_StillChoosing_IsRejected()
        {
            var dish = ScriptableObject.CreateInstance<ItemData>();
            var group = Seated(10, 60);

            Assert.IsFalse(group.TakeOrder(new[] { dish }));

            Assert.AreEqual(GroupState.Choosing, group.State);
            Assert.IsEmpty(group.Order);
            Object.DestroyImmediate(dish);
        }

        [Test]
        public void TakeOrder_Twice_SecondIsRejected()
        {
            var dish = ScriptableObject.CreateInstance<ItemData>();
            var group = Seated(10, 60);
            group.Advance(10);
            group.TakeOrder(new[] { dish });

            Assert.IsFalse(group.TakeOrder(new[] { dish }));

            Assert.AreEqual(1, group.Order.Count);
            Object.DestroyImmediate(dish);
        }

        [Test]
        public void TakeOrder_NullEmptyOrMissingDish_IsRejected()
        {
            var group = Seated(10, 60);
            group.Advance(10);

            Assert.IsFalse(group.TakeOrder(null));
            Assert.IsFalse(group.TakeOrder(new ItemData[0]));
            Assert.IsFalse(group.TakeOrder(new ItemData[] { null }));
            Assert.AreEqual(GroupState.Ready, group.State);
        }

        [Test]
        public void Advance_Ordered_PatienceStartsAgainThenLeaves()
        {
            var dish = ScriptableObject.CreateInstance<ItemData>();
            var group = Seated(10, 60);
            group.Advance(10);
            group.Advance(59);
            group.TakeOrder(new[] { dish });

            Assert.IsFalse(group.Advance(59), "Taking the order gives the table its patience back.");
            Assert.AreEqual(GroupState.Ordered, group.State);
            Assert.IsTrue(group.Advance(1));
            Assert.AreEqual(GroupState.Leaving, group.State);
            Object.DestroyImmediate(dish);
        }

        [Test]
        public void Serve_Ordered_LeavesServed()
        {
            var dish = ScriptableObject.CreateInstance<ItemData>();
            var group = Seated(10, 60);
            group.Advance(10);
            group.TakeOrder(new[] { dish });

            Assert.IsTrue(group.Serve());

            Assert.AreEqual(GroupState.Leaving, group.State);
            Assert.IsTrue(group.IsServed);
            Object.DestroyImmediate(dish);
        }

        [Test]
        public void Serve_NotOrdered_IsRejected()
        {
            var group = Seated(10, 60);

            Assert.IsFalse(group.Serve());

            Assert.AreEqual(GroupState.Choosing, group.State);
            Assert.IsFalse(group.IsServed);
        }

        [Test]
        public void Serve_Twice_SecondIsRejected()
        {
            var dish = ScriptableObject.CreateInstance<ItemData>();
            var group = Seated(10, 60);
            group.Advance(10);
            group.TakeOrder(new[] { dish });
            group.Serve();

            Assert.IsFalse(group.Serve());
            Object.DestroyImmediate(dish);
        }

        [Test]
        public void Leave_WithoutBeingServed_IsNotServed()
        {
            var dish = ScriptableObject.CreateInstance<ItemData>();
            var group = Seated(10, 60);
            group.Advance(10);
            group.TakeOrder(new[] { dish });

            group.Leave();

            Assert.IsFalse(group.IsServed);
            Object.DestroyImmediate(dish);
        }

        private TableGroup Seated(int chooseMinutes, int patienceMinutes)
        {
            var group = new TableGroup(new[] { _a }, 0, chooseMinutes, patienceMinutes);
            group.Sit();
            return group;
        }
    }
}
