using System;
using System.Collections.Generic;
using Moonbrew.Core;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class EventBusTests
    {
        private EventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
        }

        [Test]
        public void Publish_WithSubscriber_DeliversTheMessage()
        {
            var received = 0;
            _bus.Subscribe<DayStarted>(message => received = message.Day);

            _bus.Publish(new DayStarted(3));

            Assert.AreEqual(3, received);
        }

        [Test]
        public void Publish_SeveralSubscribers_CallsThemInSubscriptionOrder()
        {
            var calls = new List<string>();
            _bus.Subscribe<DayEnded>(_ => calls.Add("first"));
            _bus.Subscribe<DayEnded>(_ => calls.Add("second"));

            _bus.Publish(new DayEnded(1));

            CollectionAssert.AreEqual(new[] { "first", "second" }, calls);
        }

        [Test]
        public void Publish_OtherEventType_IsNotDelivered()
        {
            var called = false;
            _bus.Subscribe<DayStarted>(_ => called = true);

            _bus.Publish(new DayEnded(1));

            Assert.IsFalse(called);
        }

        [Test]
        public void Publish_NoSubscriber_DoesNothing()
        {
            Assert.DoesNotThrow(() => _bus.Publish(new TimeChanged(360)));
        }

        [Test]
        public void Unsubscribe_Subscriber_StopsReceiving()
        {
            var count = 0;
            Action<TimeChanged> handler = _ => count++;
            _bus.Subscribe(handler);
            _bus.Publish(new TimeChanged(360));

            _bus.Unsubscribe(handler);
            _bus.Publish(new TimeChanged(370));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Unsubscribe_NeverSubscribed_DoesNothing()
        {
            Assert.DoesNotThrow(() => _bus.Unsubscribe<DayStarted>(_ => { }));
        }

        [Test]
        public void Unsubscribe_DuringPublish_OtherHandlersStillRunThisTime()
        {
            var calls = new List<string>();
            Action<PhaseChanged> second = _ => calls.Add("second");
            _bus.Subscribe<PhaseChanged>(_ =>
            {
                calls.Add("first");
                _bus.Unsubscribe(second);
            });
            _bus.Subscribe(second);

            _bus.Publish(new PhaseChanged(1, DayPhase.Day));
            _bus.Publish(new PhaseChanged(1, DayPhase.Evening));

            CollectionAssert.AreEqual(new[] { "first", "second", "first" }, calls);
        }

        [Test]
        public void Subscribe_NullHandler_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _bus.Subscribe<DayStarted>(null));
        }

        [Test]
        public void Unsubscribe_NullHandler_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _bus.Unsubscribe<DayStarted>(null));
        }

        [Test]
        public void Clear_RemovesEverySubscriber()
        {
            var called = false;
            _bus.Subscribe<Interacted>(_ => called = true);

            _bus.Clear();
            _bus.Publish(new Interacted("farm_letterbox"));

            Assert.IsFalse(called);
        }
    }
}
