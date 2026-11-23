using System;
using Moonbrew.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Moonbrew.Cafe
{
    // A customer in the café: walks on the NavMesh, slides from the aisle onto a chair, and back.
    // Added to the customer's model when it comes in.
    [RequireComponent(typeof(NavMeshAgent))]
    public class CafeCustomer : MonoBehaviour
    {
        // Animator parameters shared with the player's controller: "Gait" 0 idle, 1 walk; "Sitting" when seated.
        private const float IdleGait = 0f;
        private const float WalkGait = 1f;

        // Below this speed (squared, m²/s²) the customer counts as standing still.
        private const float StillSpeedSqr = 0.01f;

        // The agent stops this close to its end point (meters).
        private const float ArrivalTolerance = 0.05f;

        // A seated customer is an obstacle as tall as a person sitting (meters).
        private const float SeatedHeight = 1.3f;

        // How far from a chair the aisle is searched for (meters).
        private const float AisleSearch = 2f;

        // Long enough for the animator to finish going from standing to sitting.
        private const float SitPoseSeconds = 1f;
        private static readonly int GaitHash = Animator.StringToHash("Gait");
        private static readonly int SittingHash = Animator.StringToHash("Sitting");

        private NavMeshAgent _agent;
        private NavMeshObstacle _obstacle;
        private Animator _animator;
        private Transform _head;
        private bool _canSit;
        private bool _paused;
        private float _slideSeconds;
        private Action _arrived;

        // The short move between the aisle and the chair, which is off the NavMesh.
        private Vector3 _slideFrom;
        private Vector3 _slideTo;
        private Quaternion _turnFrom;
        private Quaternion _turnTo;
        private float _slide = 1f;
        private Action _slid;
        private Vector3 _aisle;

        public CustomerData Data { get; private set; }

        public bool IsWalking => _arrived != null;

        public bool IsSeated { get; private set; }

        public void Init(CustomerData data, float speed, float radius, float slideSeconds)
        {
            Data = data;
            _slideSeconds = slideSeconds;
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = speed;
            _agent.radius = radius;
            _agent.stoppingDistance = 0f;

            // Customer models have their pivot at the feet.
            _agent.baseOffset = 0f;

            // Seated customers are avoided by the others; it never carves the NavMesh, the chair already does.
            _obstacle = gameObject.AddComponent<NavMeshObstacle>();
            _obstacle.shape = NavMeshObstacleShape.Capsule;
            _obstacle.radius = radius;
            _obstacle.height = SeatedHeight;
            _obstacle.center = Vector3.up * (SeatedHeight / 2f);
            _obstacle.carving = false;
            _obstacle.enabled = false;

            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                foreach (var parameter in _animator.parameters)
                {
                    _canSit |= parameter.nameHash == SittingHash;
                }

                _head = _animator.isHuman ? _animator.GetBoneTransform(HumanBodyBones.Head) : null;
            }
        }

        // The point above the head. The bone follows the sitting pose, whereas a skinned model's bounds stay at standing height.
        public Vector3 AboveHead(float gap)
        {
            if (_head != null)
            {
                return _head.position + Vector3.up * gap;
            }

            var point = transform.position;
            point.y = ModelBounds.Top(gameObject, point.y) + gap;
            return point;
        }

        // Walks to the reachable point closest to the destination, then calls arrived. A seated customer gets up first.
        public void WalkTo(Vector3 destination, Action arrived)
        {
            FinishSlide();
            _arrived = arrived;
            if (IsSeated)
            {
                StandUp(() => Walk(destination));
            }
            else
            {
                Walk(destination);
            }
        }

        // From the aisle where the walk ended, onto the chair.
        public void SitAt(Transform seat, Action seated = null)
        {
            _agent.enabled = false;
            _aisle = transform.position;
            SetSitting(true);
            Slide(seat.position, seat.rotation, () =>
            {
                IsSeated = true;
                _obstacle.enabled = true;
                seated?.Invoke();
            });
        }

        // Already on the chair when the owner comes back to the room.
        public void PlaceSeated(Transform seat)
        {
            _agent.enabled = false;
            _aisle = NavMesh.SamplePosition(seat.position, out var hit, AisleSearch, NavMesh.AllAreas) ? hit.position : seat.position;
            transform.SetPositionAndRotation(seat.position, seat.rotation);
            SetSitting(true);
            if (_canSit)
            {
                // Plays the transition now: order bubbles are placed from the seated head.
                _animator.Update(SitPoseSeconds);
            }

            IsSeated = true;
            _obstacle.enabled = true;
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = paused;
            }
        }

        private void Walk(Vector3 destination)
        {
            _agent.enabled = true;

            // An agent added or enabled this frame is not on the NavMesh yet, and would ignore its destination.
            if (!_agent.isOnNavMesh)
            {
                _agent.Warp(transform.position);
            }

            _agent.isStopped = _paused;
            _agent.SetDestination(destination);
        }

        private void StandUp(Action stood)
        {
            IsSeated = false;
            _obstacle.enabled = false;
            SetSitting(false);
            Slide(_aisle, transform.rotation, () =>
            {
                _agent.enabled = true;
                _agent.Warp(_aisle);
                stood();
            });
        }

        private void SetSitting(bool sitting)
        {
            if (_canSit)
            {
                _animator.SetBool(SittingHash, sitting);
            }
        }

        private void Slide(Vector3 position, Quaternion rotation, Action slid)
        {
            _slideFrom = transform.position;
            _slideTo = position;
            _turnFrom = transform.rotation;
            _turnTo = rotation;
            _slid = slid;
            _slide = 0f;
            if (_slideSeconds <= 0f)
            {
                FinishSlide();
            }
        }

        // Ends a slide at once, when the customer has to leave before reaching the chair.
        private void FinishSlide()
        {
            if (_slid == null)
            {
                return;
            }

            _slide = 1f;
            transform.SetPositionAndRotation(_slideTo, _turnTo);
            var slid = _slid;
            _slid = null;
            slid();
        }

        // Walking and sliding are checked every frame; the arrival itself is handed back to CafeFloor.
        private void Update()
        {
            if (_slid != null)
            {
                if (_paused)
                {
                    return;
                }

                _slide += Time.deltaTime / _slideSeconds;
                if (_slide >= 1f)
                {
                    FinishSlide();
                    return;
                }

                transform.SetPositionAndRotation(Vector3.Lerp(_slideFrom, _slideTo, _slide), Quaternion.Slerp(_turnFrom, _turnTo, _slide));
                return;
            }

            if (_arrived == null || !_agent.enabled)
            {
                return;
            }

            var moving = _agent.velocity.sqrMagnitude > StillSpeedSqr;
            if (_animator != null)
            {
                _animator.SetFloat(GaitHash, moving ? WalkGait : IdleGait);
            }

            // Two customers heading for the same point (the door) push each other away from it:
            // standing still that close counts as arrived.
            var left = _agent.remainingDistance;
            var arrivedThere = left <= ArrivalTolerance || (!moving && left <= _agent.radius * 2f);
            if (_agent.pathPending || !arrivedThere)
            {
                return;
            }

            var arrived = _arrived;
            _arrived = null;
            arrived();
        }
    }
}
