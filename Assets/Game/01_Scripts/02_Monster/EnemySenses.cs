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

    private void Awake()
    {
        _controller = GetComponent<EnemyController>();
    }

    // 플레이어 탐지 시도, 성공 시 true 반환
    public bool TryDetectPlayer()
    {
        if (_controller == null) return false; // 컨트롤러 없으면 실패  
        if (!_controller.IsPlayerInZone) return false; // Zone 밖이면 감지 불가

        // 이미 타겟이 있는 경우 (추적 중)
        if (_controller.CurrentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, _controller.CurrentTarget.position); // 타겟과의 거리

            if (distance > _sightRadius) // 감지 범위 이탈
            {
                _controller.ClearTarget(); // 타겟 초기화
                return false;
            }

            Vector3 dirToTarget = (_controller.CurrentTarget.position - transform.position).normalized; // 타겟 방향    
            RaycastHit rayHit;

            // 레이캐스트로 장애물 체크 
            if (Physics.Raycast(transform.position + Vector3.up, dirToTarget, out rayHit, distance))
            {
                if (!rayHit.collider.CompareTag("Player")) // 장애물에 가려짐
                {
                    _controller.ClearTarget(); // 타겟 초기화   
                    return false;
                }
            }
            return true; // 계속 추적
        }

        // 새로운 타겟 탐색
        Collider[] hits = Physics.OverlapSphere(transform.position, _sightRadius);

        // 탐지된 컬라이더들을 순회
        foreach (Collider hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue; // 자기 자신 제외

            if (hit.CompareTag("Player"))
            {
                Vector3 dirToPlayer = (hit.transform.position - transform.position).normalized; // 플레이어 방향
                float angle = Vector3.Angle(transform.forward, dirToPlayer); // 방향 벡터와의 각도

                if (angle < _fieldOfView / 2) // 시야각 내에 있음
                {
                    float distToPlayer = Vector3.Distance(transform.position, hit.transform.position); // 플레이어와의 거리
                    RaycastHit rayHit;
                    Vector3 rayOrigin = transform.position + Vector3.up; // 눈 높이

                    // 레이캐스트로 장애물 체크
                    if (Physics.Raycast(rayOrigin, dirToPlayer, out rayHit, distToPlayer))
                    {
                        if (!rayHit.collider.CompareTag("Player")) continue; // 장애물에 가려짐
                    }

                    _controller.SetTarget(hit.transform); // 타겟 설정
                    return true;
                }
            }
        }

        return false; // 플레이어 미발견
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
