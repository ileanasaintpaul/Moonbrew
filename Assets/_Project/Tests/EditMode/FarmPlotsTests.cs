using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Farming;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class FarmPlotsTests
    {
        private static readonly Vector2Int Cell = new Vector2Int(2, -1);

        [Test]
        public void Till_GrassCell_BecomesTilledAndEmpty()
        {
            var plots = new FarmPlots();

            Assert.IsTrue(plots.Till(Cell));
            Assert.IsTrue(plots.IsTilled(Cell));
            Assert.IsTrue(plots.CanPlant(Cell));
        }

        [Test]
        public void Till_AlreadyTilled_ReturnsFalse()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);

            Assert.IsFalse(plots.Till(Cell));
            Assert.AreEqual(1, plots.Count);
        }

        [Test]
        public void Plant_TilledEmptyCell_PlantsAtDayZero()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);

            Assert.IsTrue(plots.Plant(Cell, "wheat"));
            Assert.IsTrue(plots.TryGet(Cell, out var plot));
            Assert.AreEqual("wheat", plot.CropId);
            Assert.AreEqual(0, plot.DaysGrown);
            Assert.IsFalse(plots.CanPlant(Cell));
        }

        [Test]
        public void Plant_GrassCell_ReturnsFalse()
        {
            var plots = new FarmPlots();

            Assert.IsFalse(plots.Plant(Cell, "wheat"));
            Assert.IsFalse(plots.CanPlant(Cell));
        }

        [Test]
        public void Plant_CellAlreadyPlanted_KeepsTheFirstCrop()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);
            plots.Plant(Cell, "wheat");

            Assert.IsFalse(plots.Plant(Cell, "strawberry"));
            plots.TryGet(Cell, out var plot);
            Assert.AreEqual("wheat", plot.CropId);
        }

        [Test]
        public void Plant_NullOrEmptyCrop_ReturnsFalse()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);

            Assert.IsFalse(plots.Plant(Cell, null));
            Assert.IsFalse(plots.Plant(Cell, string.Empty));
            Assert.IsTrue(plots.CanPlant(Cell));
        }

        [Test]
        public void Water_TilledCell_IsWateredOnce()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);

            Assert.IsTrue(plots.Water(Cell));
            Assert.IsFalse(plots.Water(Cell), "Watering the same cell twice should do nothing.");
            plots.TryGet(Cell, out var plot);
            Assert.IsTrue(plot.Watered);
        }

        [Test]
        public void Water_GrassCell_ReturnsFalse()
        {
            Assert.IsFalse(new FarmPlots().Water(Cell));
        }

        [Test]
        public void Plant_OnWateredSoil_StaysWatered()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);
            plots.Water(Cell);

            plots.Plant(Cell, "wheat");

            plots.TryGet(Cell, out var plot);
            Assert.IsTrue(plot.Watered);
        }

        [Test]
        public void PassNight_WateredCrop_GrowsOneDayAndDries()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);
            plots.Plant(Cell, "wheat");
            plots.Water(Cell);

            var grown = plots.PassNight(id => 4);

            plots.TryGet(Cell, out var plot);
            Assert.AreEqual(1, grown);
            Assert.AreEqual(1, plot.DaysGrown);
            Assert.IsFalse(plot.Watered);
        }

        [Test]
        public void PassNight_DryCrop_Waits()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);
            plots.Plant(Cell, "wheat");

            Assert.AreEqual(0, plots.PassNight(id => 4));
            plots.TryGet(Cell, out var plot);
            Assert.AreEqual(0, plot.DaysGrown);
        }

        [Test]
        public void PassNight_RipeCrop_StopsAtItsGrowthDays()
        {
            var plots = new FarmPlots();
            plots.Restore(new List<PlotSave> { new PlotSave { x = Cell.x, y = Cell.y, cropId = "wheat", daysGrown = 4 } }, id => true);
            plots.Water(Cell);

            Assert.AreEqual(0, plots.PassNight(id => 4));
            plots.TryGet(Cell, out var plot);
            Assert.AreEqual(4, plot.DaysGrown);
        }

        [Test]
        public void PassNight_WateredEmptySoil_JustDries()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);
            plots.Water(Cell);

            Assert.AreEqual(0, plots.PassNight(id => 4));
            plots.TryGet(Cell, out var plot);
            Assert.IsFalse(plot.Watered);
        }

        [Test]
        public void PassNight_NullLookup_OnlyDries()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);
            plots.Plant(Cell, "wheat");
            plots.Water(Cell);

            Assert.AreEqual(0, plots.PassNight(null));
        }

        [Test]
        public void ToSaveThenRestore_TilledAndPlanted_KeepsBoth()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);
            plots.Till(new Vector2Int(-4, 3));
            plots.Plant(new Vector2Int(-4, 3), "strawberry");

            var restored = new FarmPlots();
            var dropped = restored.Restore(plots.ToSave(), id => true);

            Assert.AreEqual(0, dropped);
            Assert.AreEqual(2, restored.Count);
            Assert.IsTrue(restored.CanPlant(Cell));
            restored.TryGet(new Vector2Int(-4, 3), out var plot);
            Assert.AreEqual("strawberry", plot.CropId);
        }

        [Test]
        public void Restore_UnknownCrop_LeavesTheCellTilledAndEmpty()
        {
            var plots = new FarmPlots();
            var saves = new List<PlotSave> { new PlotSave { x = 1, y = 1, cropId = "removed_crop", daysGrown = 3 } };

            var dropped = plots.Restore(saves, id => id == "wheat");

            Assert.AreEqual(1, dropped);
            Assert.IsTrue(plots.CanPlant(new Vector2Int(1, 1)));
        }

        [Test]
        public void Restore_NullSave_ClearsEverything()
        {
            var plots = new FarmPlots();
            plots.Till(Cell);

            Assert.AreEqual(0, plots.Restore(null, id => true));
            Assert.AreEqual(0, plots.Count);
        }

        [Test]
        public void Restore_NegativeDays_AreClampedToZero()
        {
            var plots = new FarmPlots();

            plots.Restore(new List<PlotSave> { new PlotSave { x = 0, y = 0, cropId = "wheat", daysGrown = -5 } }, id => true);

            plots.TryGet(Vector2Int.zero, out var plot);
            Assert.AreEqual(0, plot.DaysGrown);
        }
    }
}
