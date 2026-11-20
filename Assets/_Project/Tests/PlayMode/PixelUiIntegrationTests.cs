using System.Collections;
using System.Collections.Generic;
using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // In pixel mode the interface panel is drawn into a texture at the image's resolution, one panel pixel
    // per image pixel; the panel asset gets its own settings back as soon as the renderer stops.
    public class PixelUiIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private PanelSettings _panel;
        private PixelRenderer _renderer;

        [SetUp]
        public void SetUp()
        {
            var settings = Create(ScriptableObject.CreateInstance<PixelRenderSettings>());
            _panel = Create(ScriptableObject.CreateInstance<PanelSettings>());
            _panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _panel.referenceResolution = new Vector2Int(640, 360);

            var cameraObject = Create(new GameObject("Pixel Camera"));
            cameraObject.SetActive(false);
            cameraObject.AddComponent<Camera>().orthographic = true;
            _renderer = cameraObject.AddComponent<PixelRenderer>();
            _renderer.Init(settings, default, _panel);
            cameraObject.SetActive(true);
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
        public IEnumerator PixelMode_PanelDrawsIntoATextureOfTheRenderSize()
        {
            yield return null;

            Assert.IsTrue(_renderer.IsPixelMode);
            Assert.IsNotNull(_panel.targetTexture);
            Assert.AreEqual(_renderer.RenderSize, new Vector2Int(_panel.targetTexture.width, _panel.targetTexture.height));
            Assert.AreEqual(FilterMode.Point, _panel.targetTexture.filterMode);
            Assert.AreEqual(PanelScaleMode.ConstantPixelSize, _panel.scaleMode);
            Assert.AreEqual(1f, _panel.scale);
        }

        [UnityTest]
        public IEnumerator RendererDisabled_PanelGetsItsSettingsBack()
        {
            yield return null;

            _renderer.enabled = false;
            yield return null;

            Assert.IsNull(_panel.targetTexture);
            Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, _panel.scaleMode);
        }

        private T Create<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }
    }
}
