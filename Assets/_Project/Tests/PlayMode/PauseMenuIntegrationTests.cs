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
    // The pause menu pauses the game like the recipe book does, and passes Title Screen and Quit on the bus.
    public class PauseMenuIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private readonly List<bool> _toggles = new List<bool>();
        private EventBusAsset _bus;
        private PauseMenu _menu;
        private int _titleChosen;
        private int _quitChosen;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _bus.Subscribe<GameMenuToggled>(message => _toggles.Add(message.Open));
            _bus.Subscribe<TitleScreenChosen>(message => _titleChosen++);
            _bus.Subscribe<QuitChosen>(message => _quitChosen++);

            var hud = Create(new GameObject("HUD"));
            hud.SetActive(false);
            hud.AddComponent<UIDocument>().panelSettings = Create(ScriptableObject.CreateInstance<PanelSettings>());
            _menu = hud.AddComponent<PauseMenu>();
#if UNITY_EDITOR
            _menu.Init(_bus, UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/PauseMenu.uxml"));
#else
            Assert.Ignore("The pause menu template is loaded from the editor asset database.");
#endif
            hud.SetActive(true);
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
            _toggles.Clear();
            _titleChosen = 0;
            _quitChosen = 0;
        }

        [UnityTest]
        public IEnumerator Open_PausesTheGame()
        {
            yield return null;

            _menu.Open();

            Assert.IsTrue(_menu.IsOpen);
            CollectionAssert.AreEqual(new[] { true }, _toggles);
        }

        [UnityTest]
        public IEnumerator Resume_ClosesAndResumesTheGame()
        {
            yield return null;
            _menu.Open();

            Click(_menu.ButtonNamed("resume"));

            Assert.IsFalse(_menu.IsOpen);
            CollectionAssert.AreEqual(new[] { true, false }, _toggles);
        }

        [UnityTest]
        public IEnumerator TitleScreenAndQuit_ArePassedOn()
        {
            yield return null;
            _menu.Open();

            Click(_menu.ButtonNamed("title-screen"));
            Click(_menu.ButtonNamed("quit"));

            Assert.AreEqual(1, _titleChosen);
            Assert.AreEqual(1, _quitChosen);
        }

        [UnityTest]
        public IEnumerator Open_WhileAnotherMenuIsOpen_DoesNothing()
        {
            yield return null;
            _bus.Publish(new GameMenuToggled(true));

            _menu.Open();

            Assert.IsFalse(_menu.IsOpen);
        }

        [UnityTest]
        public IEnumerator Close_WhenClosed_PublishesNothing()
        {
            yield return null;

            _menu.Close();

            Assert.IsEmpty(_toggles);
        }

        private static void Click(Button button)
        {
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
