using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    [RequireComponent(typeof(EnemyStats))]
    [RequireComponent(typeof(EnemyMovement))]

    public class EnemyController : MonoBehaviour
    {
        private EnemyStats _stats;

        private EnemyMovement _movement;

        private EnemyStateMachine _stateMachine;

        private Transform _currentTarget;


        [SerializeField] private float _detectionRange = 10f; // 감지 거리
        [SerializeField] private float _viewAngle = 90f; // 시야각

        public EnemyStats Stats { get { return _stats; } }
        public EnemyMovement Movement { get { return _movement; } }
        public Transform CurrentTarget { get { return _currentTarget; } }
        public float DetectionRange { get { return _detectionRange; } }
        public float ViewAngle { get { return _viewAngle; } }

        public string CurrentStateName
        {
            get
            {
                if (_stateMachine != null)
                {
                    return _stateMachine.CurrentStateName;
                }
                else
                {
                    return "Not Initialized";
                }
            }
        }

        private void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            _stats = GetComponent<EnemyStats>();
            _movement = GetComponent<EnemyMovement>();
            _stateMachine = new EnemyStateMachine();

            if (_stats != null)
            {
                _stats.OnDeath += HandleDeath;
            }

            _stateMachine.ChangeState(new IdleState(), this);
        }

        private void Update()
        {
            if (_stats != null && _stats.isDead) return;

            if (_stateMachine != null)
            {
                _stateMachine.Update(this);
            }
        }

        private void OnDestroy()
        {
            if (_stats != null)
            {
                _stats.OnDeath -= HandleDeath;
            }
        }

        public void ChangeState(IEnemyState newState)
        {
            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(newState, this);
            }
        }

        public void SetTarget(Transform target)
        {
            _currentTarget = target;
        }

        public void ClearTarget()
        {
            _currentTarget = null;
        }

        public bool HasTarget()
        {
            if (_currentTarget != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual void HandleDeath()
        {
            if (_movement != null)
            {
                _movement.Stop();
            }
            Debug.Log(gameObject.name + " 사망!");
        }

        public bool DetectPlayer()
        {
            float scaleFactor = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;
            float scaleRange = _detectionRange * scaleFactor;

            if (_currentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, _currentTarget.position);
                if (distance <= scaleRange)
                {
                    Vector3 dirToTarget = (_currentTarget.position - transform.position).normalized;
                    RaycastHit rayHit;

                    if (Physics.Raycast(transform.position + Vector3.up, dirToTarget, out rayHit, distance))
                    {
                        if (!rayHit.collider.CompareTag("Player"))
                        {
                            ClearTarget();
                            return false;
                        }
                    }
                }
                else
                {
                    ClearTarget();
                    return false;
                }
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, scaleRange);

            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    Vector3 dirToPlayer = (hit.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, dirToPlayer);

                    if (angle < _viewAngle / 2)
                    {

                        float distToPlayer = Vector3.Distance(transform.position, hit.transform.position);
                        RaycastHit rayHit;

                        if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, out rayHit, distToPlayer))
                        {
                            if (!rayHit.collider.CompareTag("Player"))
                            {
                                continue;
                            }
                        }

                        SetTarget(hit.transform);
                        return true;
                    }
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 스케일에 비례한 실제 감지 거리 계산
            // (x, y, z 스케일의 평균값을 사용)
            float scaleFactor = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;
            float scaledRange = _detectionRange * scaleFactor;

            // 1. 상태 텍스트
            Vector3 labelPosition = transform.position + Vector3.up * 2.5f * scaleFactor;
            UnityEditor.Handles.Label(labelPosition, "State: " + CurrentStateName);

            // 2. 감지 거리 (Detection Range) - 흰색 원 (스케일 적용)
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, scaledRange);

            // 3. 시야각 (View Angle) - 노란색 부채꼴 라인 (스케일 적용)
            Vector3 viewAngleA = DirFromAngle(-_viewAngle / 2, false);
            Vector3 viewAngleB = DirFromAngle(_viewAngle / 2, false);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + viewAngleA * scaledRange);
            Gizmos.DrawLine(transform.position, transform.position + viewAngleB * scaledRange);

            // 4. 추적 대상 연결선
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Vector3 form = transform.position + Vector3.up;
                Vector3 to = _currentTarget.position + Vector3.up;
                Gizmos.DrawLine(form, to);
                UnityEditor.Handles.Label(to, "TARGET");
            }
        }

        // 각도를 벡터로 변환하는 헬퍼 함수
        private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
#endif
    }
}
