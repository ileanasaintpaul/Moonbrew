using System;
using Moonbrew.Farming;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class FarmGridTests
    {
        [Test]
        public void CellOf_InsideACell_ReturnsItsCorner()
        {
            Assert.AreEqual(new Vector2Int(6, 4), FarmGrid.CellOf(new Vector3(6.9f, 3f, 4.1f), 1f));
        }

        [Test]
        public void CellOf_NegativeCoordinates_RoundsDown()
        {
            Assert.AreEqual(new Vector2Int(-1, -3), FarmGrid.CellOf(new Vector3(-0.2f, 0f, -2.5f), 1f));
        }

        [Test]
        public void CellOf_LargerCells_DividesBySize()
        {
            Assert.AreEqual(new Vector2Int(2, -1), FarmGrid.CellOf(new Vector3(4.5f, 0f, -0.5f), 2f));
        }

        [Test]
        public void CenterOf_Cell_IsInTheMiddleAtTheGivenHeight()
        {
            Assert.AreEqual(new Vector3(6.5f, 0.2f, -2.5f), FarmGrid.CenterOf(new Vector2Int(6, -3), 1f, 0.2f));
        }

        [Test]
        public void CellOf_ZeroOrNegativeSize_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FarmGrid.CellOf(Vector3.zero, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => FarmGrid.CenterOf(Vector2Int.zero, -1f, 0f));
        }

        [Test]
        public void InReach_NeighboursAndDiagonals_AreInReach()
        {
            var player = new Vector2Int(3, 3);

            Assert.IsTrue(FarmGrid.InReach(player, new Vector2Int(4, 3), 1));
            Assert.IsTrue(FarmGrid.InReach(player, new Vector2Int(2, 2), 1));
            Assert.IsTrue(FarmGrid.InReach(player, player, 1));
        }

        [Test]
        public void InReach_TwoCellsAway_IsOutOfReach()
        {
            Assert.IsFalse(FarmGrid.InReach(new Vector2Int(3, 3), new Vector2Int(5, 3), 1));
        }

        [Test]
        public void InReach_ZeroReach_OnlyTheSameCell()
        {
            Assert.IsTrue(FarmGrid.InReach(Vector2Int.one, Vector2Int.one, 0));
            Assert.IsFalse(FarmGrid.InReach(Vector2Int.one, Vector2Int.zero, 0));
        }

        [Test]
        public void InSwing_HalfCircle_IncludesFrontAndSidesNotBehind()
        {
            var facing = Vector3.right;

            Assert.IsTrue(FarmGrid.InSwing(Vector3.zero, facing, new Vector3(1f, 0f, 0f), 1.5f, 180f));
            Assert.IsTrue(FarmGrid.InSwing(Vector3.zero, facing, new Vector3(0f, 0f, 1f), 1.5f, 180f));
            Assert.IsFalse(FarmGrid.InSwing(Vector3.zero, facing, new Vector3(-1f, 0f, 0.1f), 1.5f, 180f));
        }

        [Test]
        public void InSwing_BeyondRadius_IsOut()
        {
            Assert.IsFalse(FarmGrid.InSwing(Vector3.zero, Vector3.right, new Vector3(2f, 0f, 0f), 1.5f, 180f));
        }

        [Test]
        public void InSwing_HeightIsIgnored()
        {
            Assert.IsTrue(FarmGrid.InSwing(Vector3.zero, Vector3.right, new Vector3(1f, 5f, 0f), 1.5f, 180f));
        }

        [Test]
        public void InSwing_NarrowArc_ExcludesTheSides()
        {
            Assert.IsFalse(FarmGrid.InSwing(Vector3.zero, Vector3.right, new Vector3(0f, 0f, 1f), 1.5f, 90f));
        }

        [Test]
        public void InSwing_NoFacingOrSameSpot_CountsAsHit()
        {
            Assert.IsTrue(FarmGrid.InSwing(Vector3.zero, Vector3.zero, new Vector3(-1f, 0f, 0f), 1.5f, 180f));
            Assert.IsTrue(FarmGrid.InSwing(Vector3.zero, Vector3.right, Vector3.zero, 1.5f, 180f));
        }
    }
}
