using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using Moonbrew.Rendering;
using Moonbrew.Ui;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // The capture mode freezes the player while the free camera moves, hides the HUD and pauses the clock.
    public class CaptureModeIntegrationTests
    {
        private const float Zoom = 8f;
        private const int Frames = 10;

        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private CaptureConfig _config;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _config = Create(ScriptableObject.CreateInstance<CaptureConfig>());
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
        public IEnumerator FreeCamera_On_PlayerStandsStillWhileTheCameraPans()
        {
            var player = CreatePlayer();
            var target = new GameObject("Follow Target").transform;
            _created.Add(target.gameObject);
            var freeCamera = CreateFreeCamera(target);
            var cameraStart = freeCamera.transform.position;

            freeCamera.SetActive(true);
            player.SetMoveInput(Vector2.up);
            var playerStart = player.transform.position;
            for (var i = 0; i < Frames; i++)
            {
                freeCamera.Pan(Vector2.right, 0.1f);
                yield return null;
            }

            Assert.AreEqual(playerStart.x, player.transform.position.x, 0.01f, "The player moved while the free camera was on.");
            Assert.AreEqual(playerStart.z, player.transform.position.z, 0.01f, "The player moved while the free camera was on.");
            Assert.Greater(Vector3.Distance(cameraStart, freeCamera.transform.position), 1f, "The free camera did not pan.");
            Assert.IsNull(freeCamera.GetComponent<CinemachineCamera>().Follow);
        }

        [UnityTest]
        public IEnumerator FreeCamera_Off_RestoresFollowZoomAndPlayerInput()
        {
            var player = CreatePlayer();
            var target = new GameObject("Follow Target").transform;
            _created.Add(target.gameObject);
            var freeCamera = CreateFreeCamera(target);
            var virtualCamera = freeCamera.GetComponent<CinemachineCamera>();

            freeCamera.SetActive(true);
            freeCamera.Zoom(1f);
            freeCamera.SetActive(false);
            player.SetMoveInput(Vector2.up);
            var playerStart = player.transform.position;
            for (var i = 0; i < Frames; i++)
            {
                yield return null;
            }

            Assert.AreSame(target, virtualCamera.Follow);
            Assert.AreEqual(Zoom, virtualCamera.Lens.OrthographicSize, 1e-4f);
            Assert.Greater(Vector3.Distance(playerStart, player.transform.position), 0.05f, "The player did not move again after the free camera.");
        }

        [UnityTest]
        public IEnumerator CaptureControls_HideHud_HidesTheDocument()
        {
            var panel = Create(ScriptableObject.CreateInstance<PanelSettings>());
            var hud = new GameObject("HUD");
            _created.Add(hud);
            var document = hud.AddComponent<UIDocument>();
            document.panelSettings = panel;
            var controls = new GameObject("Capture").AddComponent<CaptureControls>();
            _created.Add(controls.gameObject);
            controls.Init(_config, null, new[] { document });
            yield return null;

            controls.SetHudVisible(false);

            Assert.AreEqual(DisplayStyle.None, document.rootVisualElement.style.display.value);

            controls.SetHudVisible(true);

            Assert.AreEqual(DisplayStyle.Flex, document.rootVisualElement.style.display.value);
        }

        [UnityTest]
        public IEnumerator Clock_Paused_DoesNotMoveOnRealTime()
        {
            var clockConfig = Create(ScriptableObject.CreateInstance<ClockConfig>());
            var clockObject = new GameObject("Game Clock");
            clockObject.SetActive(false);
            var runner = clockObject.AddComponent<GameClockRunner>();
            runner.Init(clockConfig, _bus);
            _created.Add(clockObject);
            clockObject.SetActive(true);
            yield return null;

            runner.Clock.SetPaused(true);
            var before = runner.Clock.PhaseProgress;
            for (var i = 0; i < Frames; i++)
            {
                yield return null;
            }

            Assert.AreEqual(before, runner.Clock.PhaseProgress);
        }

        private PlayerController CreatePlayer()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 5f;
            _created.Add(ground);
            var playerConfig = Create(ScriptableObject.CreateInstance<PlayerConfig>());

            var playerObject = new GameObject("Player");
            playerObject.SetActive(false);
            playerObject.transform.position = new Vector3(0f, 0.1f, 0f);
            playerObject.AddComponent<CharacterController>().center = Vector3.up;
            var player = playerObject.AddComponent<PlayerController>();
            player.Init(playerConfig, null, _bus);
            _created.Add(playerObject);
            playerObject.SetActive(true);
            return player;
        }

        private FreeCamera CreateFreeCamera(Transform followTarget)
        {
            var cameraObject = new GameObject("Virtual Camera");
            cameraObject.SetActive(false);
            var virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
            var lens = virtualCamera.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = Zoom;
            virtualCamera.Lens = lens;
            virtualCamera.Follow = followTarget;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 30f, -40f), Quaternion.Euler(35f, 0f, 0f));
            var freeCamera = cameraObject.AddComponent<FreeCamera>();
            freeCamera.Init(_config, _bus);
            _created.Add(cameraObject);
            cameraObject.SetActive(true);
            return freeCamera;
        }

        private T Create<T>(T asset) where T : Object
        {
            _created.Add(asset);
            return asset;
        }
    }
}
