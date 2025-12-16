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
        if (_controller == null || !_controller.IsPlayerInZone) return;

        // 이미 타겟이 있는 경우 (추적 중) - 기존 유지 여부 판단
        if (_controller.CurrentTarget != null)
        {
            if (!CheckTargetVisible(_controller.CurrentTarget))
            {
               _controller.ClearTarget();
            }
            return;
        }

        // 새로운 타겟 탐색
        Collider[] hits = Physics.OverlapSphere(transform.position, _sightRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player") && CheckTargetVisible(hit.transform))
            {
                _controller.SetTarget(hit.transform);
                return; // 하나 찾으면 종료
            }
        }
    }

    // 타겟이 시야 내에 있고 장애물이 없는지 확인
    private bool CheckTargetVisible(Transform target)
    {
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > _sightRadius) return false;

        Vector3 dirToTarget = (target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToTarget);

        if (angle < _fieldOfView / 2) // 시야각 체크
        {
            // 레이캐스트 장애물 체크
            if (Physics.Raycast(transform.position + Vector3.up, dirToTarget, out RaycastHit hit, distance))
            {
                return hit.collider.CompareTag("Player");
            }
        }
        return false;
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
