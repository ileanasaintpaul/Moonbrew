using System.Collections;
using System.Collections.Generic;
using Moonbrew.Rendering;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // Moves an orthographic camera by sub-texel steps over a static scene and compares each rendered
    // frame with the previous one shifted by the camera's whole-texel motion. With snapping, the
    // image only slides; without it, edges are rasterized differently every frame (shimmer).
    public class PixelCameraIntegrationTests
    {
        private const float OrthographicSize = 9f;
        private const float CameraDistance = 60f;
        private const int Frames = 60;
        private const int WarmUpFrames = 3;
        private const float StepX = 0.37f;
        private const float StepY = 0.23f;
        private const float MaxStableMismatch = 0.001f;
        private const float MinShimmerMismatch = 0.01f;
        private const float ComparedAspect = 16f / 9f;

        private static readonly Quaternion CameraRotation = Quaternion.Euler(35f, 45f, 0f);

        private readonly List<Object> _created = new List<Object>();
        private Camera _camera;
        private PixelRenderer _renderer;
        private PixelCameraSnapper _snapper;
        private CinemachineCamera _virtualCamera;
        private float _mismatchRatio;

        [SetUp]
        public void SetUp()
        {
            var settings = ScriptableObject.CreateInstance<PixelRenderSettings>();
            _created.Add(settings);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            _created.Add(sun.gameObject);

            CreateStaticScene();

            var cameraObject = new GameObject("Pixel Camera");
            cameraObject.SetActive(false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = OrthographicSize;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.5f, 0.5f, 0.55f);
            cameraObject.AddComponent<CinemachineBrain>();
            _renderer = cameraObject.AddComponent<PixelRenderer>();
            _renderer.Init(settings);
            _snapper = cameraObject.AddComponent<PixelCameraSnapper>();
            cameraObject.SetActive(true);
            _created.Add(cameraObject);

            _virtualCamera = new GameObject("Virtual Camera").AddComponent<CinemachineCamera>();
            var lens = _virtualCamera.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = OrthographicSize;
            _virtualCamera.Lens = lens;
            _created.Add(_virtualCamera.gameObject);
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
        public IEnumerator CameraMove_SnapEnabled_RenderedImageOnlySlidesByWholeTexels()
        {
            _snapper.SnapEnabled = true;

            yield return MeasureMismatch();

            Assert.LessOrEqual(_mismatchRatio, MaxStableMismatch,
                $"{_mismatchRatio:P3} of pixels changed instead of sliding: the snapped camera shimmers.");
        }

        [UnityTest]
        public IEnumerator CameraMove_SnapDisabled_RenderedImageShimmers()
        {
            _snapper.SnapEnabled = false;

            yield return MeasureMismatch();

            Assert.GreaterOrEqual(_mismatchRatio, MinShimmerMismatch,
                $"Only {_mismatchRatio:P3} of pixels changed without snapping: the test cannot detect shimmer.");
        }

        private IEnumerator MeasureMismatch()
        {
            var size = _renderer.TextureSize;
            var texel = PixelSnap.TexelSize(OrthographicSize, size.y);
            var readback = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            _created.Add(readback);

            Color32[] previous = null;
            var previousTexel = Vector2Int.zero;
            long compared = 0;
            long mismatched = 0;

            for (var frame = 0; frame < Frames + WarmUpFrames; frame++)
            {
                var local = new Vector3(frame * StepX * texel, frame * StepY * texel, -CameraDistance);
                _virtualCamera.transform.SetPositionAndRotation(CameraRotation * local, CameraRotation);

                yield return new WaitForEndOfFrame();

                var current = Read(readback);
                var cameraLocal = Quaternion.Inverse(CameraRotation) * _camera.transform.position;
                var currentTexel = new Vector2Int(Mathf.RoundToInt(cameraLocal.x / texel), Mathf.RoundToInt(cameraLocal.y / texel));

                if (frame >= WarmUpFrames && previous != null)
                {
                    Compare(previous, current, currentTexel - previousTexel, size, ref compared, ref mismatched);
                }

                previous = current;
                previousTexel = currentTexel;
            }

            Assert.Greater(compared, 0, "No frame was compared.");
            _mismatchRatio = (float)mismatched / compared;
        }

        private Color32[] Read(Texture2D readback)
        {
            var active = RenderTexture.active;
            RenderTexture.active = _camera.targetTexture;
            readback.ReadPixels(new Rect(0, 0, readback.width, readback.height), 0, 0);
            readback.Apply(false);
            RenderTexture.active = active;
            return readback.GetPixels32();
        }

        // The camera moving by +delta texels makes the scene move by -delta in the image, so the
        // current pixel (x, y) must equal the previous pixel (x + delta.x, y + delta.y).
        // Only a central 16:9 area counts: the image widens with the Game view, and the empty sides would dilute the ratio.
        private static void Compare(Color32[] previous, Color32[] current, Vector2Int delta, Vector2Int size, ref long compared, ref long mismatched)
        {
            var side = Mathf.Max(0, (size.x - Mathf.RoundToInt(size.y * ComparedAspect)) / 2);
            for (var y = 0; y < size.y; y++)
            {
                var sourceY = y + delta.y;
                if (sourceY < 0 || sourceY >= size.y)
                {
                    continue;
                }

                for (var x = side; x < size.x - side; x++)
                {
                    var sourceX = x + delta.x;
                    if (sourceX < 0 || sourceX >= size.x)
                    {
                        continue;
                    }

                    compared++;
                    var a = current[y * size.x + x];
                    var b = previous[sourceY * size.x + sourceX];
                    if (a.r != b.r || a.g != b.g || a.b != b.b)
                    {
                        mismatched++;
                    }
                }
            }
        }

        private void CreateStaticScene()
        {
            var random = new System.Random(11);
            for (var i = 0; i < 40; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetPositionAndRotation(
                    new Vector3(Range(random, -12f, 12f), Range(random, 0f, 3f), Range(random, -12f, 12f)),
                    Quaternion.Euler(Range(random, 0f, 360f), Range(random, 0f, 360f), Range(random, 0f, 360f)));
                cube.transform.localScale = Vector3.one * Range(random, 0.3f, 2.5f);
                var material = cube.GetComponent<Renderer>().material;
                material.color = Color.HSVToRGB(Range(random, 0f, 1f), 0.6f, 0.9f);
                _created.Add(material);
                _created.Add(cube);
            }
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
