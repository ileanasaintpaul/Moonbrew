using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    public class PlayerIntegrationTests
    {
        private const float MoveSeconds = 0.6f;
        private const float TimeoutSeconds = 3f;
        private const float CameraYaw = 45f;

        private readonly List<Object> _created = new List<Object>();
        private PlayerConfig _settings;
        private PlayerController _player;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<PlayerConfig>();
            _created.Add(_settings);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 5f;
            _created.Add(ground);

            var cameraRig = new GameObject("Camera Rig");
            cameraRig.transform.rotation = Quaternion.Euler(35f, CameraYaw, 0f);
            _created.Add(cameraRig);

            var playerObject = new GameObject("Player");
            playerObject.transform.position = new Vector3(0f, 0.1f, 0f);
            var controller = playerObject.AddComponent<CharacterController>();
            controller.center = Vector3.up;
            _player = playerObject.AddComponent<PlayerController>();
            _player.Init(_settings, cameraRig.transform);
            _created.Add(playerObject);
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
        public IEnumerator Move_ForwardInput_WalksAwayFromCamera()
        {
            _player.SetMoveInput(Vector2.up);

            yield return WaitSeconds(MoveSeconds);

            var moved = _player.transform.position;
            Assert.Greater(moved.x, 0.5f, "The player should move along the camera's forward diagonal.");
            Assert.AreEqual(moved.x, moved.z, 0.05f, "Forward input should follow the 45° camera yaw.");
        }

        [UnityTest]
        public IEnumerator Move_Running_CoversMoreGroundThanWalking()
        {
            _player.SetMoveInput(Vector2.right);
            yield return WaitSeconds(MoveSeconds);
            var walked = _player.transform.position.magnitude;

            _player.transform.position = new Vector3(0f, 0.1f, 0f);
            Physics.SyncTransforms();
            _player.SetRunning(true);
            yield return WaitSeconds(MoveSeconds);
            var ran = _player.transform.position.magnitude;

            Assert.Greater(ran, walked * 1.5f);
        }

        [UnityTest]
        public IEnumerator Move_IntoWall_IsBlocked()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetPositionAndRotation(new Vector3(1.5f, 1f, 1.5f), Quaternion.Euler(0f, CameraYaw, 0f));
            wall.transform.localScale = new Vector3(6f, 2f, 0.5f);
            _created.Add(wall);

            _player.SetMoveInput(Vector2.up);
            yield return WaitSeconds(MoveSeconds * 3f);

            var distance = new Vector2(_player.transform.position.x, _player.transform.position.z).magnitude;
            Assert.Less(distance, 1.5f * Mathf.Sqrt(2f), "The player walked through the wall.");
        }

        [UnityTest]
        public IEnumerator TryInteract_ObjectInReach_InteractsWithClosest()
        {
            var bus = CreateBus();
            var interactor = _player.gameObject.AddComponent<PlayerInteractor>();
            interactor.Init(_settings, bus);
            var near = CreateInteractable(new Vector3(1f, 0.5f, 0f), "near");
            CreateInteractable(new Vector3(-1.4f, 0.5f, 0f), "far");
            string published = null;
            bus.Subscribe<Interacted>(message => published = message.InteractionId);
            yield return new WaitForFixedUpdate();

            var result = interactor.TryInteract();

            Assert.AreSame(near, result);
            Assert.AreEqual("near", published, "Interacted must be published on the bus.");
        }

        [UnityTest]
        public IEnumerator TryInteract_NothingInReach_ReturnsNull()
        {
            var bus = CreateBus();
            var interactor = _player.gameObject.AddComponent<PlayerInteractor>();
            interactor.Init(_settings, bus);
            CreateInteractable(new Vector3(5f, 0.5f, 0f), "far");
            var raised = false;
            bus.Subscribe<Interacted>(_ => raised = true);
            yield return new WaitForFixedUpdate();

            Assert.IsNull(interactor.TryInteract());
            Assert.IsFalse(raised);
        }

        private EventBusAsset CreateBus()
        {
            var bus = ScriptableObject.CreateInstance<EventBusAsset>();
            _created.Add(bus);
            return bus;
        }

        private Interactable CreateInteractable(Vector3 position, string id)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.position = position;
            box.transform.localScale = Vector3.one * 0.4f;
            _created.Add(box);
            var interactable = box.AddComponent<Interactable>();
            interactable.Init(id);
            return interactable;
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            var end = Time.time + Mathf.Min(seconds, TimeoutSeconds);
            while (Time.time < end)
            {
                yield return null;
            }
        }
    }
}
