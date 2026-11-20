using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // The lighting follows the clock: each time change on the bus relights the sun, the ambient and the windows.
    public class DayLightingIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private Light _sun;
        private Light _window;
        private Color _ambientSky;

        [SetUp]
        public void SetUp()
        {
            _ambientSky = RenderSettings.ambientSkyColor;
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _sun = Create(new GameObject("Sun")).AddComponent<Light>();
            _sun.type = LightType.Directional;
            _window = Create(new GameObject("Window")).AddComponent<Light>();
            _window.type = LightType.Spot;

            var config = Create(ScriptableObject.CreateInstance<DayLightingConfig>());
            config.Init("day_lighting_test", new[]
            {
                new LightKey { Hour = 12f, SunColor = Color.white, SunIntensity = 1f, AmbientSky = Color.white, WindowIntensity = 3f },
                new LightKey { Hour = 22f, SunColor = Color.blue, SunIntensity = 0.2f, AmbientSky = Color.black, WindowIntensity = 0f },
            });

            var holder = Create(new GameObject("Day Lighting"));
            holder.SetActive(false);
            holder.AddComponent<DayLightingDriver>().Init(config, _bus, _sun, new[] { _window });
            holder.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            RenderSettings.ambientSkyColor = _ambientSky;
            foreach (var created in _created)
            {
                Object.Destroy(created);
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator TimeChanged_Noon_LightsTheDay()
        {
            _bus.Publish(new TimeChanged(12 * 60));
            yield return null;

            Assert.AreEqual(1f, _sun.intensity, 1e-4f);
            Assert.AreEqual(Color.white, RenderSettings.ambientSkyColor);
            Assert.AreEqual(3f, _window.intensity, 1e-4f);
            Assert.IsTrue(_window.enabled);
        }

        [UnityTest]
        public IEnumerator TimeChanged_Night_DimsTheSunAndTurnsOffTheWindows()
        {
            _bus.Publish(new TimeChanged(23 * 60));
            yield return null;

            Assert.AreEqual(0.2f, _sun.intensity, 1e-4f);
            Assert.AreEqual(Color.blue, _sun.color);
            Assert.IsFalse(_window.enabled);
        }

        private T Create<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }
    }
}
