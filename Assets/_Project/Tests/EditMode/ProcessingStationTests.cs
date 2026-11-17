using System;
using Moonbrew.Core;
using Moonbrew.Kitchen;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.EditMode
{
    public class ProcessingStationTests
    {
        private ProcessData _flour;
        private ProcessData _twoForOne;

        [SetUp]
        public void SetUp()
        {
            _flour = ScriptableObject.CreateInstance<ProcessData>();
            _flour.Init("process_flour", null, 1, null, 1, 60);
            _twoForOne = ScriptableObject.CreateInstance<ProcessData>();
            _twoForOne.Init("process_double", null, 2, null, 3, 30);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_flour);
            Object.DestroyImmediate(_twoForOne);
        }

        [Test]
        public void Constructor_ZeroCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProcessingStation(0));
        }

        [Test]
        public void Load_Idle_StartsBatchesUpToTheItems()
        {
            var station = new ProcessingStation(10);

            Assert.AreEqual(4, station.Load(_flour, 4));
            Assert.IsTrue(station.IsWorking);
            Assert.AreEqual(60, station.MinutesLeft);
            Assert.AreEqual(4, station.OutputCount);
        }

        [Test]
        public void Load_MoreItemsThanCapacity_StopsAtCapacity()
        {
            Assert.AreEqual(10, new ProcessingStation(10).Load(_flour, 25));
        }

        [Test]
        public void Load_SeveralInputsPerBatch_RoundsDown()
        {
            var station = new ProcessingStation(10);

            Assert.AreEqual(2, station.Load(_twoForOne, 5));
            Assert.AreEqual(6, station.OutputCount);
        }

        [Test]
        public void Load_WhileWorking_ReturnsZero()
        {
            var station = new ProcessingStation(10);
            station.Load(_flour, 2);

            Assert.AreEqual(0, station.Load(_flour, 5));
            Assert.AreEqual(2, station.Batches);
        }

        [Test]
        public void Load_NoItemsOrNoProcess_ReturnsZero()
        {
            var station = new ProcessingStation(10);

            Assert.AreEqual(0, station.Load(_flour, 0));
            Assert.AreEqual(0, station.Load(_flour, -3));
            Assert.AreEqual(0, station.Load(null, 5));
            Assert.AreEqual(0, station.Load(_twoForOne, 1), "One item is not enough for a two-item batch.");
            Assert.IsTrue(station.IsIdle);
        }

        [Test]
        public void Advance_EnoughTime_BecomesReadyOnce()
        {
            var station = new ProcessingStation(10);
            station.Load(_flour, 1);

            Assert.IsFalse(station.Advance(50));
            Assert.IsTrue(station.Advance(20));
            Assert.IsTrue(station.IsReady);
            Assert.IsFalse(station.Advance(10), "A ready station does not finish again.");
        }

        [Test]
        public void Advance_IdleOrZeroMinutes_DoesNothing()
        {
            var station = new ProcessingStation(10);

            Assert.IsFalse(station.Advance(100));
            station.Load(_flour, 1);
            Assert.IsFalse(station.Advance(0));
            Assert.IsFalse(station.Advance(-10));
            Assert.AreEqual(60, station.MinutesLeft);
        }

        [Test]
        public void Collect_Ready_EmptiesTheStation()
        {
            var station = new ProcessingStation(10);
            station.Load(_flour, 3);
            station.Advance(60);

            Assert.IsTrue(station.Collect());
            Assert.IsTrue(station.IsIdle);
            Assert.AreEqual(0, station.OutputCount);
        }

        [Test]
        public void Collect_WorkingOrIdle_ReturnsFalse()
        {
            var station = new ProcessingStation(10);

            Assert.IsFalse(station.Collect());
            station.Load(_flour, 1);
            Assert.IsFalse(station.Collect());
        }

        [Test]
        public void ToSaveThenRestore_Working_KeepsTheBatch()
        {
            var station = new ProcessingStation(10);
            station.Load(_flour, 3);
            station.Advance(20);

            var restored = new ProcessingStation(10);
            restored.Restore(station.ToSave("station_mill"), id => id == _flour.Id ? _flour : null);

            Assert.AreEqual(_flour, restored.Process);
            Assert.AreEqual(3, restored.Batches);
            Assert.AreEqual(40, restored.MinutesLeft);
        }

        [Test]
        public void Restore_UnknownProcessOrEmptySave_LeavesIdle()
        {
            var station = new ProcessingStation(10);

            station.Restore(new StationSave { id = "station_mill", processId = "process_gone", batches = 3 }, id => null);
            Assert.IsTrue(station.IsIdle);
            station.Restore(null, id => _flour);
            Assert.IsTrue(station.IsIdle);
        }

        [Test]
        public void Restore_MoreBatchesThanCapacity_IsClamped()
        {
            var station = new ProcessingStation(5);

            station.Restore(new StationSave { processId = "process_flour", batches = 50, minutesLeft = 10 }, id => _flour);

            Assert.AreEqual(5, station.Batches);
        }
    }
}
