using System.Collections;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // Dragging across the owner on the creation screen turns it round its vertical axis only.
    public class PreviewTurntableIntegrationTests
    {
        private GameObject _model;
        private GameObject _turntableObject;
        private PreviewTurntable _turntable;

        [SetUp]
        public void SetUp()
        {
            _model = new GameObject("Owner");
            _turntableObject = new GameObject("Turntable");
            _turntableObject.SetActive(false);
            _turntable = _turntableObject.AddComponent<PreviewTurntable>();
            _turntable.Init(_model.transform);
            _turntableObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_turntableObject);
            Object.Destroy(_model);
        }

        [UnityTest]
        public IEnumerator Turn_DragRight_TurnsTheOwnerRoundItsVerticalAxis()
        {
            yield return null;

            _turntable.Turn(100f);

            Assert.AreNotEqual(0f, Mathf.DeltaAngle(0f, _model.transform.eulerAngles.y));
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, _model.transform.eulerAngles.x), 0.01f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, _model.transform.eulerAngles.z), 0.01f);
        }

        [UnityTest]
        public IEnumerator Turn_DragBackTheSameWay_ReturnsTheOwnerToItsStart()
        {
            yield return null;

            _turntable.Turn(80f);
            _turntable.Turn(-80f);

            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, _model.transform.eulerAngles.y), 0.01f);
        }
    }
}
