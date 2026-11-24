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
    // Tools tire the owner and hits hurt her; the bars show it, the night restores both and no stamina slows the walk.
    public class VitalsIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private PlayerConfig _config;
        private PlayerStamina _stamina;
        private PlayerHealth _health;
        private VitalsHud _hud;
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
            _hud = hudObject.AddComponent<VitalsHud>();
            hudObject.SetActive(true);
            _hud.Init(_bus);

            var player = Create(new GameObject("Player"));
            player.SetActive(false);
            player.AddComponent<CharacterController>();
            _controller = player.AddComponent<PlayerController>();
            _controller.Init(_config, null, _bus);
            _stamina = player.AddComponent<PlayerStamina>();
            _stamina.Init(_config, _bus);
            _health = player.AddComponent<PlayerHealth>();
            _health.Init(_config, _bus);
            player.SetActive(true);
        }

        [UnityTest]
        public IEnumerator DamageTaken_LowersHealthAndShowsIt()
        {
            yield return null;

            _bus.Publish(new DamageTaken(30));

            Assert.AreEqual(_config.MaxHealth - 30, _health.Health.Current);
            Assert.AreEqual(100f * (_config.MaxHealth - 30) / _config.MaxHealth, _hud.HealthPercent, 0.01f);
            Assert.AreEqual($"{_config.MaxHealth - 30}/{_config.MaxHealth}", _hud.HealthText);
        }

        [UnityTest]
        public IEnumerator NightPassed_HealsCompletely()
        {
            yield return null;
            _bus.Publish(new DamageTaken(500));

            _bus.Publish(new NightPassed(1, false));

            Assert.AreEqual(_config.MaxHealth, _health.Health.Current);
            Assert.AreEqual(100f, _hud.HealthPercent, 0.01f);
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_KeepHealth()
        {
            yield return null;
            _bus.Publish(new DamageTaken(20));
            var save = SaveData.CreateNew("slot_a", 1);
            _bus.Publish(new GameSaving(save));
            _bus.Publish(new DamageTaken(20));

            _bus.Publish(new GameLoaded(save, true));

            Assert.AreEqual(_config.MaxHealth - 20, _health.Health.Current);
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
            Assert.AreEqual(100f * (_config.MaxStamina - 25) / _config.MaxStamina, _hud.StaminaPercent, 0.01f);
            Assert.AreEqual($"{_config.MaxStamina - 25}/{_config.MaxStamina}", _hud.StaminaText);
        }

        [UnityTest]
        public IEnumerator NightPassed_Slept_RefillsTheBar()
        {
            yield return null;
            _bus.Publish(new ToolUsed(40));

            _bus.Publish(new NightPassed(1));

            Assert.AreEqual(_config.MaxStamina, _stamina.Stamina.Current);
            Assert.AreEqual(100f, _hud.StaminaPercent, 0.01f);
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
