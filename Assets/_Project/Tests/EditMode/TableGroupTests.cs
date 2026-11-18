using System;
using Moonbrew.Cafe;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.EditMode
{
    public class TableGroupTests
    {
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

        private TableGroup Seated(int chooseMinutes, int patienceMinutes)
        {
            var group = new TableGroup(new[] { _a }, 0, chooseMinutes, patienceMinutes);
            group.Sit();
            return group;
        }
    }
}
