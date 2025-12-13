using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// EnemySenses - 적 감지 시스템
// ============================================================================
// 
// [역할]
// 적의 "눈"과 "귀" 역할을 하는 컴포넌트입니다.
// 플레이어를 탐지하고, 장애물에 의한 시야 차단을 체크합니다.
// 
// [감지 조건]
// 플레이어가 감지되려면 다음 조건을 모두 만족해야 합니다:
// 1. 감지 범위 내에 있어야 함 (_detectRange 이내)
// 2. 시야각 내에 있어야 함 (_viewAngle/2 이내)
// 3. 중간에 장애물이 없어야 함 (Raycast 체크)
// 
// [타르코프 스타일 AI]
// - 이미 추적 중인 타겟은 감지 범위 내에 있으면 시야각 무시 (계속 추적)
// - 장애물 뒤로 숨으면 타겟 해제 (ClearTarget)
// - 새로운 타겟은 반드시 시야각 내에서만 발견 가능
// ============================================================================
public class EnemySenses : MonoBehaviour
{
    // ==================== 감지 설정 ====================
    
    // 감지 범위 (미터 단위). 이 범위 내의 플레이어만 감지할 수 있습니다.
    // 값이 크면 멀리서도 플레이어를 발견하고, 작으면 가까이 와야 발견합니다.
    [SerializeField] private float _detectRange = 30f;
    
    // 시야각 (도 단위). 정면을 기준으로 좌우로 이 각도의 절반만큼 볼 수 있습니다.
    // 예: 120도면 정면 기준 좌우 각 60도, 총 120도 범위를 볼 수 있음
    // 인간의 시야각은 약 180도, 집중 시야는 약 60도
    [SerializeField] private float _viewAngle = 120f;

    // ==================== 컴포넌트 참조 ====================
    
    // EnemyController 참조. CurrentTarget 정보와 SetTarget/ClearTarget에 필요합니다.
    private EnemyController _controller;

    // ==================== 프로퍼티 ====================
    
    // 감지 범위를 외부에서 읽을 수 있게 하는 프로퍼티 (기즈모 등에서 사용)
    public float DetectRange { get { return _detectRange; } }
    // 시야각을 외부에서 읽을 수 있게 하는 프로퍼티
    public float ViewAngle { get { return _viewAngle; } }

    // ==================== MonoBehaviour 생명주기 ====================
    
    private void Awake()
    {
        _controller = GetComponent<EnemyController>();
    }

    // ==================== 핵심 메서드 ====================
    
    // 플레이어를 찾아 타겟으로 설정하는 함수
    // 반환값: 플레이어를 볼 수 있으면 true, 없으면 false
    // 
    // [호출 시점]
    // - PatrolState, ChaseState, InvestigateState 등에서 매 프레임 호출
    // - true 반환 시 ChaseState로 전환하거나 추적 유지
    // - false 반환 시 타겟을 놓친 것으로 판단
    public bool ScanForTarget()
    {
        if (_controller == null) return false;

        // ===== 이미 타겟이 있는 경우 =====
        // 기존 타겟이 아직 유효한지 체크 (시야각은 체크하지 않음 - 이미 알고 있으니까)
        if (_controller.CurrentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, _controller.CurrentTarget.position);

            // 감지 범위 내에 있는지 체크
            if (distance <= _detectRange)
            {
                // 장애물 체크 (Line of Sight)
                // 적의 위치에서 타겟까지 레이를 쏴서 중간에 뭐가 있는지 확인
                Vector3 dirToTarget = (_controller.CurrentTarget.position - transform.position).normalized;
                RaycastHit rayHit;

                // Vector3.up을 더하는 이유: 적의 발 위치가 아닌 눈 높이에서 레이 발사
                if (Physics.Raycast(transform.position + Vector3.up, dirToTarget, out rayHit, distance))
                {
                    // 레이가 맞은 대상이 플레이어가 아니면 = 장애물에 막힘
                    if (rayHit.collider.CompareTag("Player") == false)
                    {
                        // 타겟을 볼 수 없으므로 해제
                        _controller.ClearTarget();
                        return false;
                    }
                }

                // 감지 범위 내 + 장애물 없음 = 계속 추적 가능
                return true;
            }
            else
            {
                // 감지 범위를 벗어남 = 타겟 해제
                _controller.ClearTarget();
                return false;
            }
        }

        // ===== 새로운 타겟 탐색 =====
        // 현재 타겟이 없으면 주변에서 플레이어를 찾음
        
        // OverlapSphere: 지정된 위치와 반경 내의 모든 콜라이더를 반환
        // 감지 범위 내의 모든 오브젝트를 가져옴
        Collider[] hits = Physics.OverlapSphere(transform.position, _detectRange);

        foreach (Collider hit in hits)
        {
            // 자기 자신이나 자식 오브젝트는 무시
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

            // "Player" 태그가 붙은 오브젝트만 대상
            if (hit.CompareTag("Player"))
            {
                // 플레이어 방향 벡터 계산
                Vector3 dirToPlayer = (hit.transform.position - transform.position).normalized;

                // ===== 시야각 체크 =====
                // Vector3.Angle: 두 벡터 사이의 각도를 도(degree)로 반환
                // transform.forward: 적이 바라보는 방향 (정면)
                float angle = Vector3.Angle(transform.forward, dirToPlayer);

                // 시야각의 절반보다 작으면 시야 내에 있음
                // 예: _viewAngle이 120이면, angle이 60 미만이어야 시야 내
                if (angle < _viewAngle / 2)
                {
                    float distToPlayer = Vector3.Distance(transform.position, hit.transform.position);
                    RaycastHit rayHit;

                    // 레이 시작점: 적의 눈 높이 (발 위치 + 위로 1m)
                    Vector3 rayOrigin = transform.position + Vector3.up;

                    // ===== 장애물 체크 =====
                    if (Physics.Raycast(rayOrigin, dirToPlayer, out rayHit, distToPlayer))
                    {
                        // 레이가 맞은 대상이 플레이어가 아니면 장애물에 막힘
                        if (rayHit.collider.CompareTag("Player") == false) continue;
                    }

                    // 모든 조건 만족: 타겟 설정!
                    _controller.SetTarget(hit.transform);
                    return true;
                }
            }
        }

        // 플레이어를 찾지 못함
        return false;
    }

// ==================== 에디터 전용: 시각적 디버깅 ====================
#if UNITY_EDITOR
    // OnDrawGizmos: Scene 뷰에서 감지 범위와 시야각 시각화
    private void OnDrawGizmos()
    {
        // 감지 범위: 흰색 원
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _detectRange);

        // 시야각 경계선 계산
        // DirFromAngle: 각도를 방향 벡터로 변환
        Vector3 viewAngleA = DirFormAngle(-_viewAngle / 2);  // 왼쪽 경계
        Vector3 viewAngleB = DirFormAngle(_viewAngle / 2);   // 오른쪽 경계

        // 시야각 경계선: 노란색
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * _detectRange);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * _detectRange);
    }

    // 각도를 방향 벡터로 변환하는 함수 (삼각함수 활용)
    // angleInDegrees: 변환할 각도 (도 단위)
    // 반환값: 해당 각도 방향의 단위 벡터
    // 
    // [수학 원리]
    // 단위원에서:
    // - Sin(θ) = X 좌표 (좌우)
    // - Cos(θ) = Z 좌표 (앞뒤)
    // Y는 0 (수평면에서의 방향만 계산)
    private Vector3 DirFormAngle(float angleInDegrees)
    {
        // 캐릭터의 현재 Y축 회전(transform.eulerAngles.y)을 더해서
        // 월드 좌표계 기준 방향으로 변환
        angleInDegrees += transform.eulerAngles.y;

        // Deg2Rad: 도(degree)를 라디안(radian)으로 변환
        // 삼각함수는 라디안을 사용하므로 변환 필요
        return new Vector3(
            Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),  // X: 좌우
            0,                                          // Y: 수직 (0 = 수평)
            Mathf.Cos(angleInDegrees * Mathf.Deg2Rad)   // Z: 앞뒤
        );
    }
#endif
}

