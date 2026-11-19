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
    // The clock runs on real time and the HUD follows each phase and day change.
    public class ClockHudIntegrationTests
    {
        private const float TimeoutSeconds = 5f;

        private readonly List<Object> _created = new List<Object>();
        private GameClockRunner _runner;
        private ClockHud _hud;

        [SetUp]
        public void SetUp()
        {
            var config = ScriptableObject.CreateInstance<ClockConfig>();
            _created.Add(config);
            var bus = ScriptableObject.CreateInstance<EventBusAsset>();
            _created.Add(bus);

            // The Core clock and the Ui HUD only share the bus: neither references the other.
            var clockObject = new GameObject("Game Clock");
            clockObject.SetActive(false);
            _runner = clockObject.AddComponent<GameClockRunner>();
            _runner.Init(config, bus);
            _created.Add(clockObject);

            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            _created.Add(panel);
            var hudObject = new GameObject("HUD");
            hudObject.SetActive(false);
            hudObject.AddComponent<UIDocument>().panelSettings = panel;
            _hud = hudObject.AddComponent<ClockHud>();
            _hud.Init(bus);
            _created.Add(hudObject);

            clockObject.SetActive(true);
            hudObject.SetActive(true);
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
        public IEnumerator Start_FirstFrame_ShowsFirstDayMorning()
        {
            yield return null;

            Assert.AreEqual("06:00", _hud.TimeText);
            Assert.AreEqual("Morning", _hud.PhaseText);
            Assert.AreEqual("Day 1", _hud.DayText);
        }

        [UnityTest]
        public IEnumerator Clock_PhaseEnds_HudShowsNextPhase()
        {
            yield return null;

            _runner.Clock.Tick(PhaseSeconds(DayPhase.Morning));
            yield return null;

            Assert.AreEqual("12:00", _hud.TimeText);
            Assert.AreEqual("Day", _hud.PhaseText);
            Assert.AreEqual("Day 1", _hud.DayText);
        }

        [UnityTest]
        public IEnumerator Clock_RunsOnRealTime_PhaseProgressGrows()
        {
            yield return null;
            var before = _runner.Clock.PhaseProgress;
            var end = Time.realtimeSinceStartup + Mathf.Min(0.3f, TimeoutSeconds);

            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            Assert.Greater(_runner.Clock.PhaseProgress, before);
        }

        [UnityTest]
        public IEnumerator Clock_NightEnds_HudShowsNextDay()
        {
            yield return null;
            Assert.AreEqual(DayPhase.Morning, _runner.Clock.Phase, "The test expects to start in the morning.");

            _runner.Clock.StartNextDay();
            yield return null;

            Assert.AreEqual("06:00", _hud.TimeText);
            Assert.AreEqual("Morning", _hud.PhaseText);
            Assert.AreEqual("Day 2", _hud.DayText);
        }

        private static float PhaseSeconds(DayPhase phase)
        {
            var config = ScriptableObject.CreateInstance<ClockConfig>();
            var seconds = config.PhaseDurations()[(int)phase];
            Object.Destroy(config);
            return seconds;
        }
    }
}
