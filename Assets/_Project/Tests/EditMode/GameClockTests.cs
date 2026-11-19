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
        public void StartOnDay_SavedDay_AnnouncesThatDay()
        {
            _clock.Start(7);

            Assert.AreEqual(7, _clock.Day);
            Assert.IsTrue(_clock.IsStarted);
            CollectionAssert.AreEqual(new[] { "start 7", "Morning" }, _events);
        }

        [Test]
        public void StartOnDay_AlreadyStarted_Throws()
        {
            _clock.Start();

            Assert.Throws<InvalidOperationException>(() => _clock.Start(3));
        }

        [Test]
        public void StartOnDay_ZeroOrNegative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _clock.Start(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _clock.Start(-1));
            Assert.IsFalse(_clock.IsStarted);
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

        [Test]
        public void Tick_WhilePaused_ChangesNothing()
        {
            _clock.Start();
            _events.Clear();
            _clock.SetPaused(true);

            _clock.Tick(100f);

            Assert.IsEmpty(_events);
            Assert.AreEqual(0f, _clock.PhaseProgress);
        }

        [Test]
        public void Tick_AfterResume_RunsAgain()
        {
            _clock.SetPaused(true);
            _clock.Tick(5f);
            _clock.SetPaused(false);

            _clock.Tick(5f);

            Assert.AreEqual(540, _clock.Minutes);
        }

        [Test]
        public void AdvanceMinutes_WithinPhase_MovesTheTime()
        {
            _clock.AdvanceMinutes(60);

            Assert.AreEqual(DayPhase.Morning, _clock.Phase);
            Assert.AreEqual(420, _clock.Minutes);
        }

        [Test]
        public void AdvanceMinutes_AcrossPhases_AnnouncesEachPhase()
        {
            _clock.Start();
            _events.Clear();

            _clock.AdvanceMinutes(400);

            CollectionAssert.AreEqual(new[] { "Day" }, _events);
            Assert.AreEqual(760, _clock.Minutes);
        }

        [Test]
        public void AdvanceMinutes_WholeDay_StartsTheNextMorning()
        {
            _clock.Start();
            _events.Clear();

            _clock.AdvanceMinutes(1200);

            CollectionAssert.AreEqual(new[] { "Day", "Evening", "Night", "end 1", "start 2", "Morning" }, _events);
            Assert.AreEqual(360, _clock.Minutes);
        }

        [Test]
        public void AdvanceMinutes_WhilePaused_StillAdvances()
        {
            _clock.SetPaused(true);

            _clock.AdvanceMinutes(60);

            Assert.AreEqual(420, _clock.Minutes);
        }

        [Test]
        public void AdvanceMinutes_Zero_ChangesNothing()
        {
            _clock.Start();
            _events.Clear();

            _clock.AdvanceMinutes(0);

            Assert.IsEmpty(_events);
            Assert.AreEqual(360, _clock.Minutes);
        }

        [Test]
        public void AdvanceMinutes_Negative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _clock.AdvanceMinutes(-1));
        }

        [Test]
        public void SkipToNextPhase_FromMidMorning_StartsTheDayPhase()
        {
            _clock.Tick(3f);
            _events.Clear();

            _clock.SkipToNextPhase();

            CollectionAssert.AreEqual(new[] { "Day" }, _events);
            Assert.AreEqual(720, _clock.Minutes);
        }

        [Test]
        public void SkipToNextPhase_FromNight_StartsTheNextDay()
        {
            _clock.Tick(42f);
            _events.Clear();

            _clock.SkipToNextPhase();

            CollectionAssert.AreEqual(new[] { "end 1", "start 2", "Morning" }, _events);
            Assert.AreEqual(2, _clock.Day);
        }

        [Test]
        public void SkipToNextPhase_WhilePaused_StillSkips()
        {
            _clock.SetPaused(true);

            _clock.SkipToNextPhase();

            Assert.AreEqual(DayPhase.Day, _clock.Phase);
        }

        [Test]
        public void StartAt_EveningTime_StartsInThatPhaseAtThatTime()
        {
            _clock.Start(3, 1100);

            Assert.AreEqual(3, _clock.Day);
            Assert.AreEqual(DayPhase.Evening, _clock.Phase);
            Assert.AreEqual(1100, _clock.Minutes);
            CollectionAssert.AreEqual(new[] { "start 3", "Evening" }, _events);
        }

        [Test]
        public void StartAt_ThenTick_RunsOnFromThatTime()
        {
            // The evening runs 240 minutes in 10 s: 20 minutes before the night, one second crosses it.
            _clock.Start(2, 1300);

            _clock.Tick(1f);

            Assert.AreEqual(DayPhase.Night, _clock.Phase);
            Assert.AreEqual(2, _clock.Day);
        }

        [Test]
        public void StartAt_PhaseBoundary_StartsInTheLaterPhase()
        {
            _clock.Start(1, 720);

            Assert.AreEqual(DayPhase.Day, _clock.Phase);
            Assert.AreEqual(720, _clock.Minutes);
        }

        [TestCase(359)]
        [TestCase(1560)]
        [TestCase(-10)]
        public void StartAt_TimeOutsideTheDay_Throws(int minutes)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _clock.Start(1, minutes));
            Assert.IsFalse(_clock.IsStarted);
        }

        [Test]
        public void StartAt_AlreadyStarted_ThrowsAndKeepsTheTime()
        {
            _clock.Start();

            Assert.Throws<InvalidOperationException>(() => _clock.Start(1, 1100));
            Assert.AreEqual(DayPhase.Morning, _clock.Phase);
        }

        [Test]
        public void StartAt_DayZero_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _clock.Start(0, 400));
        }
    }
}
