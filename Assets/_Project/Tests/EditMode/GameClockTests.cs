using System;
using System.Collections.Generic;
using Moonbrew.Core;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class GameClockTests
    {
        private static readonly float[] Durations = { 10f, 20f, 10f, 5f };
        private static readonly int[] Boundaries = { 360, 720, 1080, 1320, 1560 };
        private const int Step = 10;
        private const float FullDay = 45f;

        private GameClock _clock;
        private List<string> _events;

        [SetUp]
        public void SetUp()
        {
            _clock = new GameClock(Durations, Boundaries, Step, 1);
            _events = new List<string>();
            _clock.DayStarted += day => _events.Add($"start {day}");
            _clock.DayEnded += day => _events.Add($"end {day}");
            _clock.PhaseChanged += phase => _events.Add(phase.ToString());
        }

        [Test]
        public void Start_FirstCall_AnnouncesFirstDayAndMorning()
        {
            _clock.Start();

            CollectionAssert.AreEqual(new[] { "start 1", "Morning" }, _events);
        }

        [Test]
        public void Start_CalledTwice_AnnouncesOnlyOnce()
        {
            _clock.Start();
            _clock.Start();

            Assert.AreEqual(2, _events.Count);
        }

        [Test]
        public void Tick_WithinPhase_RaisesNoPhaseChange()
        {
            _clock.Start();
            _events.Clear();

            _clock.Tick(9f);

            Assert.IsEmpty(_events);
            Assert.AreEqual(DayPhase.Morning, _clock.Phase);
            Assert.AreEqual(0.9f, _clock.PhaseProgress, 1e-4f);
        }

        [Test]
        public void Tick_ReachesPhaseEnd_MovesToNextPhase()
        {
            _clock.Start();
            _events.Clear();

            _clock.Tick(10f);

            CollectionAssert.AreEqual(new[] { "Day" }, _events);
            Assert.AreEqual(DayPhase.Day, _clock.Phase);
            Assert.AreEqual(0f, _clock.PhaseProgress, 1e-4f);
        }

        [Test]
        public void Tick_LongFrame_AnnouncesEveryCrossedPhaseInOrder()
        {
            _clock.Start();
            _events.Clear();

            _clock.Tick(41f);

            CollectionAssert.AreEqual(new[] { "Day", "Evening", "Night" }, _events);
            Assert.AreEqual(1, _clock.Day);
        }

        [Test]
        public void Tick_EndOfNight_EndsDayAndStartsNextInTheMorning()
        {
            _clock.Start();
            _clock.Tick(FullDay - 1f);
            _events.Clear();

            _clock.Tick(1f);

            CollectionAssert.AreEqual(new[] { "end 1", "start 2", "Morning" }, _events);
            Assert.AreEqual(2, _clock.Day);
            Assert.AreEqual(DayPhase.Morning, _clock.Phase);
        }

        [Test]
        public void Tick_SeveralDaysAtOnce_CountsEveryDay()
        {
            _clock.Tick(FullDay * 3f);

            Assert.AreEqual(4, _clock.Day);
            Assert.AreEqual(DayPhase.Morning, _clock.Phase);
        }

        [Test]
        public void Tick_BeforeStart_StartsTheClockFirst()
        {
            _clock.Tick(0f);

            CollectionAssert.AreEqual(new[] { "start 1", "Morning" }, _events);
        }

        [Test]
        public void Tick_ZeroDelta_ChangesNothing()
        {
            _clock.Start();
            _events.Clear();

            _clock.Tick(0f);

            Assert.IsEmpty(_events);
            Assert.AreEqual(0f, _clock.PhaseProgress);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void Tick_InvalidDelta_Throws(float delta)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _clock.Tick(delta));
        }

        [Test]
        public void StartNextDay_DuringTheDay_EndsDayAndRestartsMorning()
        {
            _clock.Tick(15f);
            _events.Clear();

            _clock.StartNextDay();

            CollectionAssert.AreEqual(new[] { "end 1", "start 2", "Morning" }, _events);
            Assert.AreEqual(0f, _clock.PhaseProgress);
        }

        [Test]
        public void StartNextDay_CalledTwice_AdvancesTwoDays()
        {
            _clock.StartNextDay();
            _clock.StartNextDay();

            Assert.AreEqual(3, _clock.Day);
        }

        [Test]
        public void Constructor_NullDurations_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GameClock(null, Boundaries, Step, 1));
        }

        [Test]
        public void Constructor_WrongNumberOfDurations_Throws()
        {
            Assert.Throws<ArgumentException>(() => new GameClock(new[] { 10f, 10f }, Boundaries, Step, 1));
        }

        [TestCase(0f)]
        [TestCase(-5f)]
        [TestCase(float.PositiveInfinity)]
        public void Constructor_InvalidDuration_Throws(float duration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameClock(new[] { 10f, duration, 10f, 10f }, Boundaries, Step, 1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_FirstDayBelowOne_Throws(int firstDay)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameClock(Durations, Boundaries, Step, firstDay));
        }

        [Test]
        public void Constructor_DurationsChangedAfterwards_ClockKeepsItsCopy()
        {
            var durations = new[] { 10f, 10f, 10f, 10f };
            var clock = new GameClock(durations, Boundaries, Step, 1);
            durations[0] = 1000f;

            clock.Tick(10f);

            Assert.AreEqual(DayPhase.Day, clock.Phase);
        }

        [Test]
        public void Minutes_AtStart_IsMorningStart()
        {
            Assert.AreEqual(360, _clock.Minutes);
        }

        [Test]
        public void Minutes_HalfwayThroughMorning_IsHalfwayThroughItsHours()
        {
            _clock.Tick(5f);

            Assert.AreEqual(540, _clock.Minutes);
        }

        [Test]
        public void Minutes_BetweenSteps_RoundsDownToStep()
        {
            _clock.Tick(0.2f);

            Assert.AreEqual(360, _clock.Minutes);
        }

        [Test]
        public void Minutes_LateNight_GoesPastMidnight()
        {
            _clock.Tick(44f);

            Assert.AreEqual(1510, _clock.Minutes);
        }

        [Test]
        public void TimeChanged_OnlyWhenTheStepChanges()
        {
            var times = new List<int>();
            _clock.TimeChanged += minutes => times.Add(minutes);

            _clock.Start();
            _clock.Tick(0.2f);
            _clock.Tick(0.1f);

            CollectionAssert.AreEqual(new[] { 360, 370 }, times);
        }

        [Test]
        public void TimeChanged_NewDay_StartsBackAtMorning()
        {
            var times = new List<int>();
            _clock.Tick(44f);
            _clock.TimeChanged += minutes => times.Add(minutes);

            _clock.Tick(1f);

            CollectionAssert.AreEqual(new[] { 360 }, times);
        }

        [Test]
        public void Constructor_NullBoundaries_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GameClock(Durations, null, Step, 1));
        }

        [Test]
        public void Constructor_WrongNumberOfBoundaries_Throws()
        {
            Assert.Throws<ArgumentException>(() => new GameClock(Durations, new[] { 360, 720 }, Step, 1));
        }

        [Test]
        public void Constructor_BoundariesNotIncreasing_Throws()
        {
            Assert.Throws<ArgumentException>(() => new GameClock(Durations, new[] { 360, 720, 720, 1320, 1560 }, Step, 1));
        }

        [Test]
        public void Constructor_NegativeStart_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameClock(Durations, new[] { -60, 720, 1080, 1320, 1560 }, Step, 1));
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void Constructor_StepBelowOne_Throws(int step)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameClock(Durations, Boundaries, step, 1));
        }
    }
}
