using System.Collections;
using System.Collections.Generic;
using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // The sharp layers (the bubbles) leave the pixelated image and are drawn at screen resolution on top of it.
    public class PixelSharpLayerIntegrationTests
    {
        private const int SharpLayer = 8;
        private static readonly Color Background = new Color(0.2f, 0.3f, 0.4f, 1f);

        private readonly List<Object> _created = new List<Object>();
        private Camera _camera;
        private PixelRenderer _renderer;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var settings = ScriptableObject.CreateInstance<PixelRenderSettings>();
            _created.Add(settings);

            var cameraObject = new GameObject("Pixel Camera");
            cameraObject.SetActive(false);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Background;
            _renderer = cameraObject.AddComponent<PixelRenderer>();
            _renderer.Init(settings, 1 << SharpLayer);
            cameraObject.SetActive(true);
            _created.Add(cameraObject);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.layer = SharpLayer;
            quad.transform.localScale = Vector3.one * 4f;
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = Color.red;
            quad.GetComponent<Renderer>().sharedMaterial = material;
            _created.Add(material);
            _created.Add(quad);

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
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
        public IEnumerator SharpLayer_IsLeftOutOfThePixelImage()
        {
            Assert.AreEqual(0, _camera.cullingMask & (1 << SharpLayer));
            Assert.AreEqual(Background.r, CenterOf(_camera.targetTexture).r, 0.02f, "The pixel image should show the background.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SharpLayer_IsDrawnAtScreenScale()
        {
            var scale = PixelScaler.Compute(new Vector2Int(Screen.width, Screen.height), _renderer.RenderSize).Scale;

            Assert.IsNotNull(_renderer.SharpTexture);
            Assert.AreEqual(_renderer.TextureSize * scale, new Vector2Int(_renderer.SharpTexture.width, _renderer.SharpTexture.height));
            var center = CenterOf(_renderer.SharpTexture);
            Assert.Greater(center.r, 0.9f, "The sharp layer should be drawn.");
            Assert.Less(center.g, 0.1f);
            yield return null;
        }

        private Color CenterOf(RenderTexture texture)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.ReadPixels(new Rect(texture.width / 2, texture.height / 2, 1, 1), 0, 0);
            pixel.Apply();
            RenderTexture.active = previous;
            var color = pixel.GetPixel(0, 0);
            Object.Destroy(pixel);
            return color;
        }
    }
}
