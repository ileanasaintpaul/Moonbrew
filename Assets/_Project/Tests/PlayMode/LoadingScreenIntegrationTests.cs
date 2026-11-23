using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // The loading screen comes up when a game opens, waits for the scene, then fades and removes itself.
    public class LoadingScreenIntegrationTests
    {
        private const float TimeoutSeconds = 5f;

        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private LoadingScreen _screen;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            var data = Create(ScriptableObject.CreateInstance<LoadingScreenData>());
            data.Init(0f, 0.05f, 4);
            var panel = Create(ScriptableObject.CreateInstance<PanelSettings>());
            var screenObject = Create(new GameObject("Loading Screen"));
            screenObject.SetActive(false);
            screenObject.AddComponent<UIDocument>().panelSettings = panel;
            _screen = screenObject.AddComponent<LoadingScreen>();
            _screen.Init(data, _bus);
            screenObject.SetActive(true);
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
        public IEnumerator GameLoadStarted_ShowsTheScreenUntilTheSceneIsReady()
        {
            yield return null;

            _bus.Publish(new GameLoadStarted());
            yield return null;
            yield return null;

            Assert.IsTrue(_screen.IsShowing);
            Assert.AreEqual(1f, _screen.Alpha);
        }

        [UnityTest]
        public IEnumerator SceneReady_FadesAndRemovesTheScreen()
        {
            yield return null;
            _bus.Publish(new GameLoadStarted());

            _screen.SceneReady();
            var end = Time.realtimeSinceStartup + TimeoutSeconds;
            while (_screen != null && Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            Assert.IsTrue(_screen == null, "The screen removes itself after its fade.");
        }

        [UnityTest]
        public IEnumerator GameLoadStarted_HidesTheMenuBehind()
        {
            var menuObject = Create(new GameObject("Menu"));
            menuObject.SetActive(false);
            var menu = menuObject.AddComponent<UIDocument>();
            menu.panelSettings = _screen.GetComponent<UIDocument>().panelSettings;
            menuObject.SetActive(true);
            yield return null;

            _bus.Publish(new GameLoadStarted());

            Assert.AreEqual(DisplayStyle.None, menu.rootVisualElement.style.display.value);
        }

        [UnityTest]
        public IEnumerator SceneReady_WithoutAGameOpening_ShowsNothing()
        {
            yield return null;

            _screen.SceneReady();
            yield return null;

            Assert.IsFalse(_screen.IsShowing);
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
