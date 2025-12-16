using UnityEngine;

// 적 감지 시스템
public class EnemySenses : MonoBehaviour
{
    [SerializeField] private float _sightRadius = 30f;    // 감지 범위 (m)
    [SerializeField] private float _fieldOfView = 120f;   // 시야각 (도)

    private EnemyController _controller;  // 컨트롤러 참조

    // 프로퍼티, 외부에서 읽기 전용 
    public float SightRadius => _sightRadius;
    public float FieldOfView => _fieldOfView;

    [SerializeField] private float _searchInterval = 0.2f; // 검색 주기 (초)

    private void Awake()
    {
        _controller = GetComponent<EnemyController>();
    }

    private void OnEnable()
    {
        StartCoroutine(SearchRoutine());
    }

    // 주기적으로 플레이어 탐색 (최적화)
    private System.Collections.IEnumerator SearchRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_searchInterval);

        while (true)
        {
            yield return wait;
            DetectPlayer();
        }
    }

    // 플레이어 탐지 로직 (내부 호출용)
    private void DetectPlayer()
    {        
        if (_controller == null) return;
        
        // Normal 몬스터: Zone 진입 시에만 감지 (EnemySpawner가 타겟 설정)
        // Epic 몬스터: Zone과 무관하게 항상 감지 (자체 탐색)
        if (!_controller.IsEpic && !_controller.IsPlayerInZone) return;

        // 이미 타겟이 있는 경우 (추적 중) - 유형별 다른 처리
        if (_controller.CurrentTarget != null)
        {
            // 일반 몬스터: Zone 내에서는 무조건 타겟 유지 (시야각/거리 무시)
            if (!_controller.IsEpic) return;
            
            // Epic 몬스터: 거리 체크만 (추적 중이므로 시야각은 무시)
            float distance = Vector3.Distance(transform.position, _controller.CurrentTarget.position);
            if (distance > _sightRadius)
            {
                _controller.ClearTarget(); // 거리 초과 시에만 타겟 해제
            }
            return;
        }

        // 새로운 타겟 탐색
        Collider[] hits = Physics.OverlapSphere(transform.position, _sightRadius);
        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            
            // Epic: FOV 체크 필요, Normal: Zone 내 360° 탐지
            bool ignoreFOV = !_controller.IsEpic;
            if (CheckTargetVisible(hit.transform, ignoreFOV: ignoreFOV))
            {
                _controller.SetTarget(hit.transform);
                return;
            }
        }
    }

    // 타겟이 시야 내에 있고 장애물이 없는지 확인
    // ignoreFOV: true면 시야각 무시하고 거리 + 장애물만 체크 (360° 탐지)
    private bool CheckTargetVisible(Transform target, bool ignoreFOV = false)
    {
        // 가슴 높이에서 가슴 높이로 레이캐스트 (바닥/발 충돌 방지)
        Vector3 eyePos = transform.position + Vector3.up;
        Vector3 targetCenter = target.position + Vector3.up;
        
        // XZ 평면 거리로 계산 (Y축 무시 - 고저차 영향 제거)
        Vector3 flatEyePos = new Vector3(eyePos.x, 0, eyePos.z);
        Vector3 flatTargetPos = new Vector3(targetCenter.x, 0, targetCenter.z);
        float horizontalDistance = Vector3.Distance(flatEyePos, flatTargetPos);
        
        if (horizontalDistance > _sightRadius)
        {
            return false;
        }

        Vector3 dirToTarget = (targetCenter - eyePos).normalized;
        
        // FOV 체크 (ignoreFOV가 true면 건너뜀) - XZ 평면 기준
        if (!ignoreFOV)
        {
            Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 flatDirToTarget = new Vector3(dirToTarget.x, 0, dirToTarget.z).normalized;
            float angle = Vector3.Angle(flatForward, flatDirToTarget);
            if (angle >= _fieldOfView / 2)
            {
                return false;
            }
        }

        // 레이캐스트로 장애물 체크 (3D - 실제 장애물은 고려해야 함)
        float rayDistance = Vector3.Distance(eyePos, targetCenter);
        if (Physics.Raycast(eyePos, dirToTarget, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.CompareTag("Player");
        }
        // 레이캐스트가 아무것도 안 맞음 = 장애물 없음 = 시야 확보
        return true;
    }

#if UNITY_EDITOR
    // 감지 범위 및 시야각 시각화
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white; // 감지 범위 색상
        Gizmos.DrawWireSphere(transform.position, _sightRadius); // 감지 범위

        Vector3 viewAngleA = DirFormAngle(-_fieldOfView / 2); // 시야각 좌측
        Vector3 viewAngleB = DirFormAngle(_fieldOfView / 2); // 시야각 우측

        Gizmos.color = Color.yellow; // 시야각 색상

        // 시야각 좌우 끝점
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * _sightRadius); 
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * _sightRadius); 
    }

    // 각도를 방향 벡터로 변환
    private Vector3 DirFormAngle(float angleInDegrees)
    {
        angleInDegrees += transform.eulerAngles.y; // 캐릭터 회전 반영

        // 삼각함수로 2D 방향 벡터 계산 (XZ 평면)
        // Unity에서 Z+ = 전방, X+ = 우측
        // Sin(각도) = X축 성분 (좌우), Cos(각도) = Z축 성분 (전후)
        // 예: 0도 → (0, 0, 1) = 정면, 90도 → (1, 0, 0) = 우측
        return new Vector3(
            Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), // X축 (좌우 방향)
            0,                                         // Y축 (수직, 사용 안함)
            Mathf.Cos(angleInDegrees * Mathf.Deg2Rad)  // Z축 (전후 방향)
        );
    }
#endif
}
