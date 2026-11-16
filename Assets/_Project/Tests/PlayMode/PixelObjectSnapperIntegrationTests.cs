using System.Collections;
using System.Collections.Generic;
using Moonbrew.Rendering;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // A moving object's model must sit on the camera's texel grid every frame, so it moves by whole pixels.
    public class PixelObjectSnapperIntegrationTests
    {
        private const float OrthographicSize = 9f;
        private const float CameraDistance = 60f;
        private const float GridTolerance = 1e-3f;
        private const int Frames = 20;
        private const float Step = 0.0137f;

        private static readonly Quaternion CameraRotation = Quaternion.Euler(35f, 45f, 0f);

        private readonly List<Object> _created = new List<Object>();
        private PixelRenderer _renderer;
        private PixelCameraSnapper _cameraSnapper;
        private Transform _root;
        private Transform _visual;
        private PixelObjectSnapper _objectSnapper;

        [SetUp]
        public void SetUp()
        {
            var settings = ScriptableObject.CreateInstance<PixelRenderSettings>();
            _created.Add(settings);

            var cameraObject = new GameObject("Pixel Camera");
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = OrthographicSize;
            cameraObject.AddComponent<CinemachineBrain>();
            _renderer = cameraObject.AddComponent<PixelRenderer>();
            _renderer.Init(settings);
            _cameraSnapper = cameraObject.AddComponent<PixelCameraSnapper>();
            cameraObject.SetActive(true);
            _created.Add(cameraObject);

            var virtualCamera = new GameObject("Virtual Camera").AddComponent<CinemachineCamera>();
            var lens = virtualCamera.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = OrthographicSize;
            virtualCamera.Lens = lens;
            virtualCamera.transform.SetPositionAndRotation(CameraRotation * new Vector3(0.123f, 0.456f, -CameraDistance), CameraRotation);
            _created.Add(virtualCamera.gameObject);

            _root = new GameObject("Moving Object").transform;
            _root.position = new Vector3(0.31f, 0f, 0.77f);
            _visual = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
            _visual.SetParent(_root, false);
            _objectSnapper = _root.gameObject.AddComponent<PixelObjectSnapper>();
            _objectSnapper.Init(_cameraSnapper, _visual);
            _created.Add(_root.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator Move_SubTexelSteps_VisualStaysOnTexelGrid()
        {
            var texel = PixelSnap.TexelSize(OrthographicSize, _renderer.TextureSize.y);
            for (var frame = 0; frame < Frames; frame++)
            {
                _root.position += new Vector3(Step, 0f, Step * 0.5f);
                var rootBefore = _root.position;

                yield return new WaitForEndOfFrame();

                var local = Quaternion.Inverse(CameraRotation) * _visual.position;
                Assert.AreEqual(0f, Remainder(local.x, texel), GridTolerance, $"Frame {frame}: x is off the texel grid.");
                Assert.AreEqual(0f, Remainder(local.y, texel), GridTolerance, $"Frame {frame}: y is off the texel grid.");
                Assert.AreEqual(rootBefore, _root.position, "The root must keep its exact position.");
                Assert.Less(Vector3.Distance(_visual.position, _root.position), texel, "The visual drifted more than a texel.");
            }
        }

        [UnityTest]
        public IEnumerator Disable_RestoresVisualRestPosition()
        {
            yield return new WaitForEndOfFrame();

            _objectSnapper.enabled = false;

            Assert.AreEqual(Vector3.zero, _visual.localPosition);
        }

        // Distance to the nearest grid line, in texels.
        private static float Remainder(float value, float texel)
        {
            var texels = value / texel;
            return Mathf.Abs(texels - Mathf.Round(texels));
        }
    }
}
