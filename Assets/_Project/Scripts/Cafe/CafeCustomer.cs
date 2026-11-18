using System;
using UnityEngine;
using UnityEngine.AI;

namespace Moonbrew.Cafe
{
    // A customer in the café: walks on the NavMesh, then sits on a chair. Added to the customer's model when it comes in.
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
        private static readonly int GaitHash = Animator.StringToHash("Gait");
        private static readonly int SittingHash = Animator.StringToHash("Sitting");

        private NavMeshAgent _agent;
        private Animator _animator;
        private bool _canSit;
        private Action _arrived;

        public CustomerData Data { get; private set; }

        public bool IsWalking => _arrived != null;

        public bool IsSeated { get; private set; }

        public void Init(CustomerData data, float speed)
        {
            Data = data;
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = speed;
            _agent.stoppingDistance = 0f;

            // Customer models have their pivot at the feet.
            _agent.baseOffset = 0f;
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                foreach (var parameter in _animator.parameters)
                {
                    _canSit |= parameter.nameHash == SittingHash;
                }
            }
        }

        // Walks to the reachable point closest to the destination, then calls arrived.
        public void WalkTo(Vector3 destination, Action arrived)
        {
            StandUp();
            _arrived = arrived;

            // An agent added this frame is not on the NavMesh yet, and would ignore its destination.
            if (!_agent.isOnNavMesh)
            {
                _agent.Warp(transform.position);
            }

            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }

        public void SitAt(Transform seat)
        {
            _agent.enabled = false;
            transform.SetPositionAndRotation(seat.position, seat.rotation);
            IsSeated = true;
            if (_canSit)
            {
                _animator.SetBool(SittingHash, true);
            }
        }

        public void SetPaused(bool paused)
        {
            if (_agent.enabled)
            {
                _agent.isStopped = paused;
            }
        }

        private void StandUp()
        {
            if (!IsSeated)
            {
                return;
            }

            IsSeated = false;
            if (_canSit)
            {
                _animator.SetBool(SittingHash, false);
            }

            _agent.enabled = true;
            _agent.Warp(transform.position);
        }

        // Walking is checked every frame; the arrival itself is handed back to CafeFloor.
        private void Update()
        {
            if (_arrived == null)
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
