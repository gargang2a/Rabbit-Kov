using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 적의 감지 시스템
    // 플레이어를 시야각과 거리로 감지하고, 장애물이 있는지 레이캐스트로 확인함
    
    public class EnemySenses : MonoBehaviour
    {
        [Header("감지 설정")]
        // _detectionRange: 감지 가능한 최대 거리 (단위: 미터)
        // 이 범위 밖의 플레이어는 아무리 눈앞에 있어도 감지 안 됨
        [SerializeField] private float _detectionRange = 10f;
        
        // _viewAngle: 시야각 (단위: 도)
        // 90이면 앞쪽 90도 범위만 감지 (좌우 45도씩)
        // 360이면 모든 방향 감지 (레이더처럼)
        [SerializeField] private float _viewAngle = 90f;

        // _controller: EnemyController 참조
        // 왜 필요? 타겟을 설정하려면 컨트롤러에 접근해야 함
        private EnemyController _controller;

        // 프로퍼티: 외부에서 값을 읽을 수 있게 함
        public float DetectionRange { get { return _detectionRange; } }
        public float ViewAngle { get { return _viewAngle; } }

        private void Awake()
        {
            _controller = GetComponent<EnemyController>();
        }

        // ScanForTarget: 플레이어를 찾는 함수
        // 반환값: 발견하면 true, 못 찾으면 false
        // 발견하면 자동으로 SetTarget도 호출됨
        public bool ScanForTarget()
        {
            if (_controller == null) return false;

            // 스케일에 따라 감지 범위 조정
            // 왜? 적이 커지면 감지 범위도 커져야 자연스러움
            // lossyScale: 부모 오브젝트 스케일까지 포함한 실제 크기
            float scaleFactor = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;
            float scaledRange = _detectionRange * scaleFactor;

            // 이미 타겟이 있으면 유지할 수 있는지 확인
            if (_controller.CurrentTarget != null)
            {
                // Vector3.Distance(A, B): A와 B 사이의 거리를 계산
                float distance = Vector3.Distance(transform.position, _controller.CurrentTarget.position);
                
                // 범위 안에 있는지 확인
                if (distance <= scaledRange)
                {
                    // 장애물이 있는지 레이캐스트로 확인
                    // 레이캐스트: 광선을 쏴서 뭔가에 맞는지 확인하는 기능
                    // FPS 게임의 총알 판정에도 사용됨
                    
                    // 방향 계산: (목표 - 나) = 나에서 목표를 향하는 벡터
                    // normalized: 길이를 1로 만듦 (방향만 남김)
                    Vector3 dirToTarget = (_controller.CurrentTarget.position - transform.position).normalized;
                    
                    // RaycastHit: 레이캐스트가 맞은 정보를 저장하는 구조체
                    // 맞은 위치, 맞은 오브젝트 등의 정보가 들어있음
                    RaycastHit rayHit;

                    // Physics.Raycast(시작위치, 방향, out 결과, 거리)
                    // 시작위치: 내 위치에서 1미터 위 (눈높이)
                    // out: 함수가 이 변수에 결과를 저장함
                    // 반환값: 뭔가에 맞으면 true
                    if (Physics.Raycast(transform.position + Vector3.up, dirToTarget, out rayHit, distance))
                    {
                        // 맞은 것이 플레이어가 아니면 장애물이 있는 것
                        // CompareTag: 태그 비교 함수 (== "Player"보다 빠름)
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
                    // 범위 밖이면 타겟 해제
                    _controller.ClearTarget();
                    return false;
                }
            }

            // 새로운 타겟 찾기
            // Physics.OverlapSphere: 구 안에 있는 모든 Collider를 찾음
            // 반환값: Collider 배열
            Collider[] hits = Physics.OverlapSphere(transform.position, scaledRange);

            // foreach: 배열의 모든 요소를 순회하는 반복문
            // for보다 간결하고 읽기 쉬움
            foreach (Collider hit in hits)
            {
                // 자기 자신은 제외
                // IsChildOf: 자식 오브젝트인지 확인 (무기 등 자식도 제외)
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                
                // Player 태그가 있는지 확인
                if (hit.CompareTag("Player"))
                {
                    // 방향 계산
                    Vector3 dirToPlayer = (hit.transform.position - transform.position).normalized;
                    
                    // Vector3.Angle(A, B): A와 B 사이의 각도 (단위: 도)
                    // transform.forward: 내가 바라보는 방향 (앞쪽)
                    float angle = Vector3.Angle(transform.forward, dirToPlayer);

                    // 시야각 안에 있는지 확인
                    // viewAngle이 90도면 앞쪽 ±45도 범위만 감지
                    if (angle < _viewAngle / 2)
                    {
                        // 장애물 확인 (위와 동일한 레이캐스트)
                        float distToPlayer = Vector3.Distance(transform.position, hit.transform.position);
                        RaycastHit rayHit;
                        Vector3 rayOrigin = transform.position + Vector3.up;

                        if (Physics.Raycast(rayOrigin, dirToPlayer, out rayHit, distToPlayer))
                        {
                            if (rayHit.collider.CompareTag("Player") == false) continue;
                        }

                        // 발견! 타겟으로 설정
                        _controller.SetTarget(hit.transform);
                        return true;
                    }
                }
            }

            return false;
        }

#if UNITY_EDITOR
        // OnDrawGizmos: Scene 뷰에서 항상 보이는 시각화
        private void OnDrawGizmos()
        {
            float scaleFactor = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;
            float scaledRange = _detectionRange * scaleFactor;

            // 감지 범위 (흰색 원)
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, scaledRange);

            // 시야각 (노란색 선)
            Vector3 viewAngleA = DirFromAngle(-_viewAngle / 2);
            Vector3 viewAngleB = DirFromAngle(_viewAngle / 2);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + viewAngleA * scaledRange);
            Gizmos.DrawLine(transform.position, transform.position + viewAngleB * scaledRange);
        }

        // DirFromAngle: 각도를 방향 벡터로 변환하는 함수
        // 시야각 시각화에 사용
        private Vector3 DirFromAngle(float angleInDegrees)
        {
            // 현재 오브젝트의 Y축 회전값을 더함
            angleInDegrees += transform.eulerAngles.y;
            
            // 삼각함수로 방향 계산
            // Mathf.Deg2Rad: 도를 라디안으로 변환 (삼각함수는 라디안 사용)
            // Sin: X 좌표, Cos: Z 좌표
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
#endif
    }
}
