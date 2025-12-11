using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RabbitKov.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]

    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private float _walkSpeed = 2f;
        [SerializeField] private float _runSpeed = 5f;
        [SerializeField] private Transform[] _patrolPoints;
        [SerializeField] private float _randomPatrolRadius = 10f;
        [SerializeField] private bool _useRandomPatrol = true;

        private int _currentPatrolIndex = 0;
        private NavMeshAgent _agent;

        public float WalkSpeed { get { return _walkSpeed; } }
        public float RunSpeed { get { return _runSpeed; } }

        public bool HasReachedDestination
        {
            get
            {
                bool arrived = _agent.remainingDistance <= _agent.stoppingDistance;
                bool notCalculation = _agent.pathPending == false;
                return arrived && notCalculation;
            }
        }

        public bool isMoving
        {
            get
            {
                float speedSquared = _agent.velocity.sqrMagnitude;
                return speedSquared > 0.01f;
            }
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        public void MoveTo(Vector3 destination)
        {
            if (_agent == null) return;
            if (_agent.isOnNavMesh == false) return;

            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }

        public virtual void MoveTo(Transform target)
        {
            if (target == null) return;

            MoveTo(target.position);
        }

        public void Stop()
        {
            if (_agent == null) return;
            if (_agent.isOnNavMesh == false) return;

            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        public void SetSpeed(float speed)
        {
            if (_agent != null) _agent.speed = speed;
        }

        public void SetWalkSpeed()
        {
            SetSpeed(_walkSpeed);
        }

        public void SetRunSpeed()
        {
            SetSpeed(_runSpeed);
        }

        public bool MoveToNextPatrolPoint()
        {
            if (_patrolPoints == null) return false;
            if (_patrolPoints.Length == 0) return false;

            Transform targetPoint = _patrolPoints[_currentPatrolIndex];

            if (targetPoint != null)
            {
                SetWalkSpeed();
                MoveTo(targetPoint.position);
            }

            return true;
        }

        public void AdvancePatrolIndex()
        {
            if (_patrolPoints == null) return;
            if (_patrolPoints.Length == 0) return;

            _currentPatrolIndex = _currentPatrolIndex + 1;

            if (_currentPatrolIndex >= _patrolPoints.Length)
            {
                _currentPatrolIndex = 0;
            }
        }

        public bool HasPatrolPoint()
        {
            if (_patrolPoints == null) return false;
            if (_patrolPoints.Length > 0) return true;
            return false;
        }

        public bool UseRandomPatrol
        {
            get { return _useRandomPatrol; }
        }

        public bool CanPatrol()
        {
            if (_useRandomPatrol)
            {
                return true;
            }

            return HasPatrolPoint();
        }

        public bool MoveToRandomPoint()
        {
            if (_agent == null) return false;
            if (_agent.isOnNavMesh == false) return false;

            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * _randomPatrolRadius;
            randomDirection = randomDirection + transform.position;
            randomDirection.y = transform.position.y;

            NavMeshHit hit;
            bool foundPosition = NavMesh.SamplePosition(randomDirection, out hit, 2f, NavMesh.AllAreas);

            if (foundPosition == false) return false;

            SetWalkSpeed();
            MoveTo(hit.position);

            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_patrolPoints == null) return;
            if (_patrolPoints.Length < 2) return;

            Gizmos.color = Color.cyan;

            for (int i = 0; i < _patrolPoints.Length; i++)
            {
                if (_patrolPoints[i] == null) continue;

                Gizmos.DrawWireSphere(_patrolPoints[i].position, 0.5f);

                int nextIndex = i + 1;
                if (nextIndex >= _patrolPoints.Length)
                {
                    nextIndex = 0;
                }

                if (_patrolPoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(_patrolPoints[i].position, _patrolPoints[nextIndex].position);
                }
            }
        }
#endif
    }
}