using Moonbrew.Farming;
using Moonbrew.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class CropCatalogTests
    {
        private ItemData _wheatSeeds;
        private ItemData _flour;
        private CropData _wheat;
        private CropCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _wheatSeeds = ScriptableObject.CreateInstance<ItemData>();
            _flour = ScriptableObject.CreateInstance<ItemData>();
            _wheat = ScriptableObject.CreateInstance<CropData>();
            _wheat.Init("wheat", _wheatSeeds, null, 4, new GameObject[0]);
            _catalog = ScriptableObject.CreateInstance<CropCatalog>();
            _catalog.Init(new[] { null, _wheat });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_wheatSeeds);
            Object.DestroyImmediate(_flour);
            Object.DestroyImmediate(_wheat);
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void Find_KnownId_ReturnsTheCrop()
        {
            Assert.AreEqual(_wheat, _catalog.Find("wheat"));
        }

        [Test]
        public void Find_UnknownOrEmptyId_ReturnsNull()
        {
            Assert.IsNull(_catalog.Find("pumpkin"));
            Assert.IsNull(_catalog.Find(null));
            Assert.IsNull(_catalog.Find(string.Empty));
        }

        [Test]
        public void ForSeed_SeedOfACrop_ReturnsThatCrop()
        {
            Assert.AreEqual(_wheat, _catalog.ForSeed(_wheatSeeds));
        }

        [Test]
        public void ForSeed_NotASeedOrNull_ReturnsNull()
        {
            Assert.IsNull(_catalog.ForSeed(_flour));
            Assert.IsNull(_catalog.ForSeed(null));
        }
    }
}
