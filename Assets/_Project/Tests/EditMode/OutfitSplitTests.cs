using System;
using System.Collections.Generic;
using Moonbrew.Player;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class OutfitSplitTests
    {
        private readonly List<int> _top = new List<int>();
        private readonly List<int> _bottom = new List<int>();

        [Test]
        public void Split_TriangleWithMostCornersOnTheLegs_IsBottom()
        {
            var triangles = new[] { 0, 1, 2, 2, 3, 4, 1, 3, 4 };
            var onLegs = new[] { false, false, false, true, true };

            OutfitSplit.Split(triangles, onLegs, _top, _bottom);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, _top, "No corner on the legs.");
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 1, 3, 4 }, _bottom, "Two corners on the legs.");
        }

        [Test]
        public void Split_OneCornerOnTheLegs_StaysTop()
        {
            OutfitSplit.Split(new[] { 0, 1, 2 }, new[] { true, false, false }, _top, _bottom);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, _top);
            Assert.IsEmpty(_bottom);
        }

        [Test]
        public void Split_NoTriangles_ClearsBothParts()
        {
            _top.Add(7);
            _bottom.Add(8);

            OutfitSplit.Split(new int[0], new bool[0], _top, _bottom);

            Assert.IsEmpty(_top);
            Assert.IsEmpty(_bottom);
        }

        [Test]
        public void Split_IncompleteLastTriangle_IsIgnored()
        {
            OutfitSplit.Split(new[] { 0, 1, 2, 0, 1 }, new[] { true, true, true }, _top, _bottom);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, _bottom);
        }

        [Test]
        public void Split_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => OutfitSplit.Split(null, new bool[0], _top, _bottom));
            Assert.Throws<ArgumentNullException>(() => OutfitSplit.Split(new int[0], null, _top, _bottom));
            Assert.Throws<ArgumentNullException>(() => OutfitSplit.Split(new int[0], new bool[0], null, _bottom));
        }
    }
}
