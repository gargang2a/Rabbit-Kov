using UnityEngine;

// [역할] 적 감지 시스템 - 시야 범위 및 FOV 기반 플레이어 탐지
public class EnemySenses : MonoBehaviour
{
    [Header("Fallback Settings (DataSO Overrides)")]
    [SerializeField] private float _sightRadius = 30f;    // 감지 범위 (m)
    [SerializeField] private float _fieldOfView = 120f;   // 시야각 (도)
    [Tooltip("시야 차단 장애물 레이어")]
    [SerializeField] private LayerMask _obstacleMask;     // 장애물 레이어

    private EnemyController _controller; // 컨트롤러 참조

    public float SightRadius => _sightRadius;   // 감지 범위 프로퍼티
    public float FieldOfView => _fieldOfView;   // 시야각 프로퍼티

    [SerializeField] private float _searchInterval = 0.2f; // 검색 주기 (초)

    private void Awake()
    {
        _controller = GetComponent<EnemyController>(); // 컨트롤러 캐싱
        
        if (_controller?.EnemyData != null) // DataSO가 있으면
        {
            Initialize(_controller.EnemyData); // 초기화
        }
    }
    
    // EnemyDataSO 기반 초기화
    public void Initialize(EnemyDataSO data)
    {
        _sightRadius = data.sightRadius;   // 감지 범위 적용
        _fieldOfView = data.fieldOfView;   // 시야각 적용
    }

    private void OnEnable()
    {
        StartCoroutine(SearchRoutine()); // 탐색 코루틴 시작
    }

    // 주기적 플레이어 탐색 (최적화)
    private System.Collections.IEnumerator SearchRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_searchInterval); // 대기 시간 캐싱

        while (true)
        {
            yield return wait;  // 대기
            DetectPlayer();     // 탐지 실행
        }
    }

    // 플레이어 탐지
    private void DetectPlayer()
    {        
        if (_controller == null) return; // 컨트롤러 없으면 종료
        
        bool isNormal = !_controller.RestrictToZone; // Normal 여부
        
        // 이미 타겟이 있는 경우
        if (_controller.CurrentTarget != null)
        {
            // Normal: 타겟 무조건 유지
            if (isNormal)
            {
                return; // 타겟 유지
            }
            
            // Epic/Boss: 보스전 중이면 타겟 유지
            BossController boss = _controller as BossController; // 보스인지 확인
            if (boss != null && boss.IsBossFight)
            {
                return; // 보스전 중이면 타겟 유지
            }
            
            // 거리 체크
            float distance = Vector3.Distance(transform.position, _controller.CurrentTarget.position); // 거리 계산
            if (distance > _sightRadius) // 감지 범위 밖이면
            {
                Debug.LogWarning($"[Senses] {name}: Epic/Boss - 거리 초과로 타겟 해제 (Dist: {distance:F1} > {_sightRadius})");
                _controller.ClearTarget(); // 타겟 해제
            }
            return;
        }
        
        // Normal이 타겟 없는 상태
        if (isNormal)
        {
            Debug.LogWarning($"[Senses] {name}: Normal - 타겟 없음! (IsPlayerInZone: {_controller.IsPlayerInZone})");
        }
        
        // Zone 내부에서만 탐색
        if (!_controller.IsPlayerInZone) return; // Zone 밖이면 종료

        // 새 타겟 탐색
        Collider[] hits = Physics.OverlapSphere(transform.position, _sightRadius); // 범위 내 콜라이더
        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player")) continue; // 플레이어 아니면 스킵
            
            bool ignoreFOV = isNormal; // Normal은 360도 탐지
            if (CheckTargetVisible(hit.transform, ignoreFOV: ignoreFOV)) // 시야 체크
            {
                Debug.Log($"[Senses] {name}: 새 타겟 발견! -> {hit.name}");
                _controller.SetTarget(hit.transform); // 타겟 설정
                return;
            }
        }
    }

    // 시야 내 + 장애물 없는지 확인
    private bool CheckTargetVisible(Transform target, bool ignoreFOV = false)
    {
        Vector3 eyePos = transform.position + Vector3.up;       // 눈 위치 (가슴 높이)
        Vector3 targetCenter = target.position + Vector3.up;    // 타겟 중심
        
        // XZ 평면 거리 (고저차 무시)
        Vector3 flatEyePos = new Vector3(eyePos.x, 0, eyePos.z);         // 눈 위치 (Y=0)
        Vector3 flatTargetPos = new Vector3(targetCenter.x, 0, targetCenter.z); // 타겟 위치 (Y=0)
        float horizontalDistance = Vector3.Distance(flatEyePos, flatTargetPos); // 수평 거리
        
        if (horizontalDistance > _sightRadius) return false; // 거리 초과

        Vector3 dirToTarget = (targetCenter - eyePos).normalized; // 타겟 방향
        
        // FOV 체크
        if (!ignoreFOV) // FOV 무시 아니면
        {
            Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;    // 전방 (XZ)
            Vector3 flatDirToTarget = new Vector3(dirToTarget.x, 0, dirToTarget.z).normalized;            // 타겟 방향 (XZ)
            float angle = Vector3.Angle(flatForward, flatDirToTarget);                                     // 각도 계산
            if (angle >= _fieldOfView / 2) // 시야각 밖이면
            {
                Debug.Log($"[EnemySenses] {name}: 시야각 벗어남");
                return false;
            }
        }

        // 장애물 체크
        float rayDistance = Vector3.Distance(eyePos, targetCenter);                                      // 레이 거리
        if (Physics.Raycast(eyePos, dirToTarget, out RaycastHit hit, rayDistance, _obstacleMask))        // 장애물 있으면
        {
            Debug.Log($"[EnemySenses] {name}: 장애물에 가려짐 ({hit.collider.name})");
            return false;
        }
        
        return true; // 시야 확보
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 감지 범위
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _sightRadius); // 흰색 원

        // 시야각
        Vector3 viewAngleA = DirFormAngle(-_fieldOfView / 2); // 왼쪽 시야
        Vector3 viewAngleB = DirFormAngle(_fieldOfView / 2);  // 오른쪽 시야

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * _sightRadius); // 왼쪽 선
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * _sightRadius); // 오른쪽 선
    }

    // 각도 -> 방향 벡터 변환
    private Vector3 DirFormAngle(float angleInDegrees)
    {
        angleInDegrees += transform.eulerAngles.y; // 회전 반영

        return new Vector3(
            Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), // X (좌우)
            0,                                          // Y (수직)
            Mathf.Cos(angleInDegrees * Mathf.Deg2Rad)  // Z (전후)
        );
    }
#endif
}
