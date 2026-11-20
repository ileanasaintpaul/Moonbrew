using System.Collections;
using System.Collections.Generic;
using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // With the interface pixelated, each text is hidden in the low-resolution panel and drawn again,
    // sharp, on the overlay; when the pixelated interface stops, the texts come back as they were.
    public class SharpTextOverlayIntegrationTests
    {
        private const int Frames = 5;

        private readonly List<Object> _created = new List<Object>();
        private PixelRenderer _renderer;
        private SharpTextOverlay _overlay;
        private Label _label;

        [SetUp]
        public void SetUp()
        {
            var settings = Create(ScriptableObject.CreateInstance<PixelRenderSettings>());
            var panel = Create(ScriptableObject.CreateInstance<PanelSettings>());

            var cameraObject = Create(new GameObject("Pixel Camera"));
            cameraObject.SetActive(false);
            cameraObject.AddComponent<Camera>().orthographic = true;
            _renderer = cameraObject.AddComponent<PixelRenderer>();
            _renderer.Init(settings, default, panel);
            cameraObject.SetActive(true);

            var hud = Create(new GameObject("HUD"));
            hud.SetActive(false);
            hud.AddComponent<UIDocument>().panelSettings = panel;
            _overlay = hud.AddComponent<SharpTextOverlay>();
            _overlay.Init(_renderer);
            hud.SetActive(true);

            _label = new Label("06:00");
            _label.style.width = 60f;
            _label.style.height = 20f;
            hud.GetComponent<UIDocument>().rootVisualElement.Add(_label);
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
        public IEnumerator PixelatedInterface_TextIsHiddenAndDrawnSharp()
        {
            yield return WaitFrames();

            Assert.IsTrue(SharpText.IsHidden(_label.text));
            Assert.IsNotNull(_overlay.SharpCopyOf(_label));
        }

        [UnityTest]
        public IEnumerator TextChanged_SharpCopyFollows()
        {
            yield return WaitFrames();

            _label.text = "06:10";
            yield return WaitFrames();

            Assert.AreEqual("06:10", _overlay.SharpCopyOf(_label).text);
        }

        [UnityTest]
        public IEnumerator RendererDisabled_TextComesBack()
        {
            yield return WaitFrames();

            _renderer.enabled = false;
            yield return WaitFrames();

            Assert.AreEqual("06:00", _label.text);
        }

        private static IEnumerator WaitFrames()
        {
            for (var i = 0; i < Frames; i++)
            {
                yield return null;
            }
        }

        private T Create<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }
    }
}
