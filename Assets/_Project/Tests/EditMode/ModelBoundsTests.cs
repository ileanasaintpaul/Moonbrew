using Moonbrew.Core;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class ModelBoundsTests
    {
        private GameObject _model;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_model);
        }

        [Test]
        public void Top_ModelWithChildren_IsTheHighestPoint()
        {
            _model = new GameObject("Model");
            var low = GameObject.CreatePrimitive(PrimitiveType.Cube);
            low.transform.SetParent(_model.transform, false);
            var high = GameObject.CreatePrimitive(PrimitiveType.Cube);
            high.transform.SetParent(_model.transform, false);
            high.transform.localPosition = new Vector3(0f, 2f, 0f);

            Assert.AreEqual(2.5f, ModelBounds.Top(_model, 0f), 1e-4f);
        }

        [Test]
        public void Top_NothingDrawn_ReturnsTheFallback()
        {
            _model = new GameObject("Empty");

            Assert.AreEqual(0.3f, ModelBounds.Top(_model, 0.3f));
        }

        [Test]
        public void Top_NullModel_ReturnsTheFallback()
        {
            Assert.AreEqual(-1f, ModelBounds.Top(null, -1f));
        }
    }
}
