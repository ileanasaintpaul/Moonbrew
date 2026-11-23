using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class CreationCameraTests
    {
        [Test]
        public void FieldOfViewFor_WideScreen_KeepsTheFieldOfView()
        {
            Assert.AreEqual(30f, CreationCamera.FieldOfViewFor(30f, 2.5f, 2f), 0.001f);
        }

        [Test]
        public void FieldOfViewFor_DesignAspect_KeepsTheFieldOfView()
        {
            Assert.AreEqual(30f, CreationCamera.FieldOfViewFor(30f, 2f, 2f), 0.001f);
        }

        [Test]
        public void FieldOfViewFor_NarrowScreen_KeepsTheHorizontalView()
        {
            var fieldOfView = CreationCamera.FieldOfViewFor(30f, 1f, 2f);

            var expected = Mathf.Tan(15f * Mathf.Deg2Rad) * 2f;
            Assert.AreEqual(expected, Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) * 1f, 0.001f);
        }

        [Test]
        public void FieldOfViewFor_ZeroOrNegativeAspect_KeepsTheFieldOfView()
        {
            Assert.AreEqual(30f, CreationCamera.FieldOfViewFor(30f, 0f, 2f), 0.001f);
            Assert.AreEqual(30f, CreationCamera.FieldOfViewFor(30f, -1f, 2f), 0.001f);
        }
    }
}
