using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 적 감지 시스템 - 시야각/거리로 플레이어 감지, 레이캐스트로 장애물 확인
    public class EnemySenses : MonoBehaviour
    {
        [Header("감지 설정")]
        [SerializeField] private float _detectionRange = 10f;  // 감지 거리
        [SerializeField] private float _viewAngle = 90f;       // 시야각 (도)

        private EnemyController _controller;

        public float DetectionRange { get { return _detectionRange; } }
        public float ViewAngle { get { return _viewAngle; } }

        private void Awake()
        {
            _controller = GetComponent<EnemyController>();
        }

        // 플레이어 찾기 (발견시 true, 자동으로 SetTarget 호출)
        public bool ScanForTarget()
        {
            if (_controller == null) return false;

            // 스케일에 따라 범위 조정
            float scaleFactor = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;
            float scaledRange = _detectionRange * scaleFactor;

            // 이미 타겟 있으면 유지 가능한지 확인
            if (_controller.CurrentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, _controller.CurrentTarget.position);
                
                if (distance <= scaledRange)
                {
                    // 장애물 확인 (레이캐스트)
                    Vector3 dirToTarget = (_controller.CurrentTarget.position - transform.position).normalized;
                    RaycastHit rayHit;

                    if (Physics.Raycast(transform.position + Vector3.up, dirToTarget, out rayHit, distance))
                    {
                        if (rayHit.collider.CompareTag("Player") == false)
                        {
                            _controller.ClearTarget();
                            return false;
                        }
                    }
                    
                    return true;
                }
                else
                {
                    _controller.ClearTarget();
                    return false;
                }
            }

            // 새 타겟 찾기
            Collider[] hits = Physics.OverlapSphere(transform.position, scaledRange);

            foreach (Collider hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                
                if (hit.CompareTag("Player"))
                {
                    Vector3 dirToPlayer = (hit.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, dirToPlayer);

                    // 시야각 안에 있는지
                    if (angle < _viewAngle / 2)
                    {
                        // 장애물 확인
                        float distToPlayer = Vector3.Distance(transform.position, hit.transform.position);
                        RaycastHit rayHit;
                        Vector3 rayOrigin = transform.position + Vector3.up;

                        if (Physics.Raycast(rayOrigin, dirToPlayer, out rayHit, distToPlayer))
                        {
                            if (rayHit.collider.CompareTag("Player") == false) continue;
                        }

                        _controller.SetTarget(hit.transform);
                        return true;
                    }
                }
            }

            return false;
        }

#if UNITY_EDITOR
        // Scene 뷰 시각화
        private void OnDrawGizmos()
        {
            float scaleFactor = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;
            float scaledRange = _detectionRange * scaleFactor;

            // 감지 범위 (흰색)
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, scaledRange);

            // 시야각 (노란색)
            Vector3 viewAngleA = DirFromAngle(-_viewAngle / 2);
            Vector3 viewAngleB = DirFromAngle(_viewAngle / 2);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + viewAngleA * scaledRange);
            Gizmos.DrawLine(transform.position, transform.position + viewAngleB * scaledRange);
        }

        private Vector3 DirFromAngle(float angleInDegrees)
        {
            angleInDegrees += transform.eulerAngles.y;
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
#endif
    }
}
