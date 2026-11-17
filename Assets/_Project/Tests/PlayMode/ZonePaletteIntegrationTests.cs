using System.Collections;
using System.Collections.Generic;
using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // The zone palette is drawn by the pixel output and can be switched off.
    public class ZonePaletteIntegrationTests
    {
        private const string ShaderName = "Moonbrew/PaletteFilter";

        private readonly List<Object> _created = new List<Object>();
        private PixelRenderer _renderer;
        private ZonePaletteFilter _filter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var settings = Create(ScriptableObject.CreateInstance<PixelRenderSettings>());
            var palette = Create(ScriptableObject.CreateInstance<ZonePalette>());
            var shader = Shader.Find(ShaderName);
            Assert.IsNotNull(shader, $"Shader {ShaderName} not found.");
            var material = Create(new Material(shader));

            var cameraObject = Create(new GameObject("Palette Camera"));
            cameraObject.SetActive(false);
            cameraObject.AddComponent<Camera>();
            _renderer = cameraObject.AddComponent<PixelRenderer>();
            _renderer.Init(settings);
            _filter = cameraObject.AddComponent<ZonePaletteFilter>();
            _filter.Init(palette, material);
            cameraObject.SetActive(true);
            yield return null;
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

        [Test]
        public void Start_PaletteOn_OutputDrawsWithThePaletteShader()
        {
            Assert.IsTrue(_filter.IsActive);
            Assert.IsNotNull(_renderer.OutputMaterial);
            Assert.AreEqual(ShaderName, _renderer.OutputMaterial.shader.name);
        }

        [UnityTest]
        public IEnumerator SetActive_Off_OutputReturnsToDefaultMaterial()
        {
            _filter.SetActive(false);
            yield return null;

            Assert.IsFalse(_filter.IsActive);
            Assert.IsNull(_renderer.OutputMaterial);
        }

        [UnityTest]
        public IEnumerator SetActive_OffThenOn_PaletteComesBack()
        {
            _filter.SetActive(false);
            yield return null;
            _filter.SetActive(true);
            yield return null;

            Assert.IsTrue(_filter.IsActive);
            Assert.AreEqual(ShaderName, _renderer.OutputMaterial.shader.name);
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
