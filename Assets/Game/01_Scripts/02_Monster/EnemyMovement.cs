using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RabbitKov.Enemy
{
    // 적 이동 시스템 - NavMeshAgent로 길찾기/이동 처리
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        [Header("이동 속도")]
        [SerializeField] private float _walkSpeed = 2f;  // 걷기 (정찰용)
        [SerializeField] private float _runSpeed = 5f;   // 뛰기 (추격용)

        [Header("정찰 설정")]
        [SerializeField] private Transform[] _patrolPoints;       // 웨이포인트 배열
        [SerializeField] private float _randomPatrolRadius = 10f; // 랜덤 정찰 범위
        [SerializeField] private bool _useRandomPatrol = true;    // true면 랜덤, false면 웨이포인트

        [Header("회전 설정")]
        [SerializeField] private float _rotateSpeed = 120f;  // 초당 회전 각도

        private int _currentPatrolIndex = 0;  // 현재 웨이포인트 인덱스
        private NavMeshAgent _agent;

        public float WalkSpeed { get { return _walkSpeed; } }
        public float RunSpeed { get { return _runSpeed; } }
        public float RotationSpeed { get { return _rotateSpeed; } }
        public bool UseRandomPatrol { get { return _useRandomPatrol; } }

        // 목적지 도착 확인
        public bool HasReachedDestination
        {
            get
            {
                if (_agent == null) return false;
                if (_agent.pathPending) return false;
                
                if (_agent.hasPath == false)
                {
                    return _agent.velocity.sqrMagnitude < 0.01f;
                }
                
                return _agent.remainingDistance <= _agent.stoppingDistance;
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

        // 목적지로 이동
        public void MoveTo(Vector3 destination)
        {
            if (_agent == null) return;
            if (_agent.isOnNavMesh == false) return;

            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }

        public void MoveTo(Transform target)
        {
            if (target == null) return;
            MoveTo(target.position);
        }

        // 멈춤
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

        public void SetWalkSpeed() { SetSpeed(_walkSpeed); }
        public void SetRunSpeed() { SetSpeed(_runSpeed); }

        // 타겟 바라보기 (완료시 true)
        public bool LookAt(Vector3 target)
        {
            Vector3 direction = (target - transform.position).normalized;
            direction.y = 0;  // 수평 회전만

            if (direction == Vector3.zero) return true;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotateSpeed * Time.deltaTime);

            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            return angleDifference < 1f;
        }

        public bool LookAt(Transform target)
        {
            if (target == null) return true;
            return LookAt(target.position);
        }

        public bool HasPatrolPoint()
        {
            if (_patrolPoints == null) return false;
            if (_patrolPoints.Length > 0) return true;
            return false;
        }

        public bool CanPatrol()
        {
            if (_useRandomPatrol == true) return true;
            return HasPatrolPoint();
        }

        // 현재 웨이포인트로 이동
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

        // 다음 웨이포인트 인덱스로 (끝이면 0으로)
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

        // 랜덤 위치로 이동
        public bool MoveToRandomPoint()
        {
            if (_agent == null) return false;
            if (_agent.isOnNavMesh == false) return false;

            Vector3 randomDirection = Random.insideUnitSphere * _randomPatrolRadius;
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
        // Scene 뷰 시각화
        private void OnDrawGizmos()
        {
            DrawRandomPatrolGizmos();
            DrawPatrolPointGizmos();
            DrawPathGizmos();
        }

        private void DrawRandomPatrolGizmos()
        {
            if (_useRandomPatrol == false) return;
            
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _randomPatrolRadius);
        }

        private void DrawPatrolPointGizmos()
        {
            if (_patrolPoints == null) return;
            if (_patrolPoints.Length < 2) return;

            Gizmos.color = Color.cyan;

            for (int i = 0; i < _patrolPoints.Length; i++)
            {
                if (_patrolPoints[i] == null) continue;

                Gizmos.DrawWireSphere(_patrolPoints[i].position, 0.5f);

                int nextIndex = i + 1;
                if (nextIndex >= _patrolPoints.Length) nextIndex = 0;

                if (_patrolPoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(_patrolPoints[i].position, _patrolPoints[nextIndex].position);
                }
            }
        }

        private void DrawPathGizmos()
        {
            if (_agent == null) return;
            if (_agent.hasPath == false) return;

            Gizmos.color = Color.green;
            Vector3[] corners = _agent.path.corners;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }

            if (corners.Length > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(corners[corners.Length - 1], 0.5f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + _agent.velocity);
        }
#endif
    }
}