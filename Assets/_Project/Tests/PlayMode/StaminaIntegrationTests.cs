using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // Tools tire the owner, the bar shows it, the night restores it and an empty bar slows the walk.
    public class StaminaIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private PlayerConfig _config;
        private PlayerStamina _stamina;
        private StaminaHud _hud;
        private PlayerController _controller;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _config = Create(ScriptableObject.CreateInstance<PlayerConfig>());
            var panel = Create(ScriptableObject.CreateInstance<PanelSettings>());
            var hudObject = Create(new GameObject("HUD"));
            hudObject.SetActive(false);
            hudObject.AddComponent<UIDocument>().panelSettings = panel;
            _hud = hudObject.AddComponent<StaminaHud>();
            hudObject.SetActive(true);
            _hud.Init(_bus);

            var player = Create(new GameObject("Player"));
            player.SetActive(false);
            player.AddComponent<CharacterController>();
            _controller = player.AddComponent<PlayerController>();
            _controller.Init(_config, null, _bus);
            _stamina = player.AddComponent<PlayerStamina>();
            _stamina.Init(_config, _bus);
            player.SetActive(true);
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
        public IEnumerator ToolUsed_SpendsItsCostAndShowsIt()
        {
            yield return null;

            _bus.Publish(new ToolUsed(25));

            Assert.AreEqual(_config.MaxStamina - 25, _stamina.Stamina.Current);
            Assert.AreEqual(75f, _hud.FillPercent, 0.01f);
        }

        [UnityTest]
        public IEnumerator NightPassed_Slept_RefillsTheBar()
        {
            yield return null;
            _bus.Publish(new ToolUsed(40));

            _bus.Publish(new NightPassed(1));

            Assert.AreEqual(_config.MaxStamina, _stamina.Stamina.Current);
            Assert.AreEqual(100f, _hud.FillPercent, 0.01f);
        }

        [UnityTest]
        public IEnumerator NightPassed_NotSlept_WakesTired()
        {
            yield return null;

            _bus.Publish(new NightPassed(1, false));

            Assert.AreEqual(_config.MaxStamina - _config.MissedSleepPenalty, _stamina.Stamina.Current);
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_KeepTheStamina()
        {
            yield return null;
            _bus.Publish(new ToolUsed(10));
            var save = SaveData.CreateNew("slot_a", 1);
            _bus.Publish(new GameSaving(save));
            _bus.Publish(new ToolUsed(10));

            _bus.Publish(new GameLoaded(save, true));

            Assert.AreEqual(_config.MaxStamina - 10, _stamina.Stamina.Current);
        }

        [UnityTest]
        public IEnumerator EmptyStamina_SlowsTheWalk()
        {
            yield return null;
            _controller.SetMoveInput(Vector2.up);
            yield return null;
            var rested = _controller.Velocity.magnitude;

            _bus.Publish(new ToolUsed(_config.MaxStamina));
            yield return null;

            Assert.AreEqual(rested * _config.ExhaustedSpeedFactor, _controller.Velocity.magnitude, 0.01f);
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
