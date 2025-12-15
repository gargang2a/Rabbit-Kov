using UnityEngine;
using UnityEngine.AI;

// 적 이동 시스템 - NavMesh 기반 이동 및 회전 관리
public class EnemyMovement : MonoBehaviour
{
    [Header("이동 속도")]
    [SerializeField] private float _patrolSpeed = 3.5f;       // 정찰 속도 (m/s)
    [SerializeField] private float _chaseSpeed = 5.5f;        // 추격 속도 (m/s)
    [SerializeField] private float _rotateSpeed = 120f;       // 회전 속도 (도/초)

    [Header("정찰 설정")]
    [Tooltip("최소 정찰 이동 거리 (m)")]
    [SerializeField] private float _minPatrolDistance = 2f;   // 최소 정찰 거리
    [Tooltip("최대 정찰 이동 거리 (m)")]
    [SerializeField] private float _maxPatrolDistance = 20f;  // 최대 정찰 거리
    [SerializeField] private bool _useRandomPatrol = true;    // 랜덤 정찰 사용 여부
    
    [Header("도착 판정")]
    [Tooltip("목적지까지 이 거리 이내면 도착으로 판정 (m)")]
    [SerializeField] private float _arrivalThreshold = 0.5f;  // 도착 임계값 (m)

    private NavMeshAgent _agent;   // NavMesh 에이전트
    private Collider _boundZone;   // 이동 제한 Zone

    // 프로퍼티, 외부에서 읽기 전용
    public float PatrolSpeed => _patrolSpeed;
    public float ChaseSpeed => _chaseSpeed;
    public float MinPatrolDistance => _minPatrolDistance;
    public float MaxPatrolDistance => _maxPatrolDistance;
    public bool UseRandomPatrol => _useRandomPatrol;

    // 목적지 도착 여부
    public bool HasReachedDestination
    {
        get
        {
            if (_agent == null) return false;
            if (_agent.pathPending) return false; // 경로 계산 중
            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid) return false; // 경로 실패

            // 유효 도착 거리 계산 (stoppingDistance가 0이면 _arrivalThreshold 사용)
            float effectiveArrivalDist = Mathf.Max(_agent.stoppingDistance, _arrivalThreshold);

            // 경로 없으면 정지 상태 체크
            if (_agent.hasPath == false)
            {
                return _agent.velocity.sqrMagnitude < 0.01f;
            }

            // 부분 경로인 경우 정지 상태 체크
            if (_agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                if (_agent.velocity.sqrMagnitude < 0.01f) return true;
            }

            // 핵심 조건: 남은 거리가 임계값 이하이면 도착
            if (_agent.remainingDistance <= effectiveArrivalDist) return true;

            // 추가 안전장치: 속도 0 + 가까운 거리 = 도착 (에이전트가 멈춘 경우)
            if (_agent.velocity.sqrMagnitude < 0.01f && _agent.remainingDistance < 1f) return true;

            return false;
        }
    }

    // 이동 중 여부
    public bool IsMoving => _agent.velocity.sqrMagnitude > 0.01f;

    // 컴포넌트 캐싱
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        EnsureOnNavMesh(); // NavMesh 위 보정
        
        // 일반 몬스터만 겹침 허용 (메가봉크 스타일)
        var controller = GetComponent<EnemyController>();
        if (controller != null && !controller.IsEpic)
        {
            _agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
        }
    }

    // Zone 할당 (정찰 범위 제한용)
    public void SetBoundZone(Collider zone)
    {
        _boundZone = zone;
    }

    // Zone 내부 여부 확인
    private bool IsInsideZone(Vector3 position)
    {
        if (_boundZone == null) return true; // Zone 없으면 제한 없음
        return _boundZone.bounds.Contains(position);
    }

    // 위치를 Zone 내부로 제한
    private Vector3 ClampToZone(Vector3 position)
    {
        if (_boundZone == null) return position;
        return _boundZone.bounds.ClosestPoint(position);
    }

    // NavMesh 위로 위치 보정
    private void EnsureOnNavMesh()
    {
        if (_agent == null || _agent.isOnNavMesh) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
        {
            _agent.enabled = false;
            transform.position = hit.position;
            _agent.enabled = true;
        }
        else
        {
            Debug.LogWarning(gameObject.name + ": 근처에 NavMesh를 찾을 수 없음!");
        }
    }

    // 지정 위치로 이동
    public void MoveTo(Vector3 destination)
    {
        if (_agent == null) return;

        // NavMesh 위가 아니면 보정
        if (_agent.isOnNavMesh == false)
        {
            EnsureOnNavMesh();
            if (_agent.isOnNavMesh == false) return;
        }

        // 목적지를 NavMesh 위로 보정
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 5f, NavMesh.AllAreas))
        {
            destination = hit.position;
        }

        _agent.isStopped = false;
        _agent.SetDestination(destination);
    }

    // 정지
    public void Stop()
    {
        if (_agent == null || _agent.isOnNavMesh == false) return;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
    }

    // 속도 설정
    public void SetSpeed(float speed)
    {
        if (_agent != null) _agent.speed = speed;
    }

    // 속도 프리셋
    public void SetPatrolSpeed() => _agent.speed = _patrolSpeed; // 정찰 속도
    public void SetChaseSpeed() => _agent.speed = _chaseSpeed;   // 추격 속도

    // 타겟 방향으로 회전, 완료 시 true 반환
    public bool FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction.magnitude < 0.01f) return true; // 거의 같은 위치면 완료

        // 타겟과의 각도 계산
        // SignedAngle: 전방 벡터와 목표 방향 사이 각도 (-180 ~ 180)
        float angleToTarget = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        float rotateThisFrame = _rotateSpeed * Time.deltaTime;

        // 남은 각도가 이번 프레임 회전량보다 작으면 정확히 맞춤
        if (Mathf.Abs(angleToTarget) < rotateThisFrame)
        {
            rotateThisFrame = Mathf.Abs(angleToTarget);
        }

        float rotationDirection = Mathf.Sign(angleToTarget); // 회전 방향 (+1 또는 -1)
        transform.Rotate(0, rotationDirection * rotateThisFrame, 0);

        return Mathf.Abs(angleToTarget) < rotateThisFrame; // 회전 완료 여부
    }

    // 오버로드: Transform 타겟
    public bool FaceTarget(Transform target)
    {
        if (target == null) return true;
        return FaceTarget(target.position);
    }

    // 정찰 가능 여부
    public bool CanPatrol() => _useRandomPatrol;

    // 랜덤 정찰 시작, 성공 시 true 반환
    public bool StartRandomPatrol()
    {
        if (_agent == null || _agent.isOnNavMesh == false) return false;

        int maxAttempts = 30; // 최대 시도 횟수

        // 유효한 정찰 위치 탐색
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 랜덤 방향 및 거리 생성 (최소 ~ 최대 범위)
            Vector3 randomDirection = Random.onUnitSphere;
            float randomDistance = Random.Range(_minPatrolDistance, _maxPatrolDistance);
            randomDirection *= randomDistance;
            randomDirection += transform.position; // 상대 좌표 → 절대 좌표
            randomDirection.y = transform.position.y; // 높이 고정

            if (!IsInsideZone(randomDirection)) continue; // Zone 밖이면 재시도

            // NavMesh 위 유효 위치 탐색
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 10f, NavMesh.AllAreas))
            {
                if (!IsInsideZone(hit.position)) continue; // Zone 재확인

                float distance = Vector3.Distance(transform.position, hit.position);
                if (distance < _minPatrolDistance) continue; // 최소 거리 미달 시 재시도

                SetPatrolSpeed();
                MoveTo(hit.position);
                return true;
            }
        }

        return false; // 유효 위치 탐색 실패
    }

#if UNITY_EDITOR
    // 이동 경로 및 정찰 범위 시각화 (에디터 전용)
    private void OnDrawGizmos()
    {
        DrawPatrolRadiusGizmos(); // 정찰 범위
        DrawPathGizmos();         // 이동 경로
    }

    // 랜덤 정찰 범위 그리기 (최소/최대)
    private void DrawPatrolRadiusGizmos()
    {
        if (!_useRandomPatrol) return; // 랜덤 정찰 미사용 시 패스

        // 최대 정찰 범위 (시안색)
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _maxPatrolDistance);
        
        // 최소 정찰 범위 (노란색)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _minPatrolDistance);
    }

    // 경로 및 속도 벡터 그리기
    private void DrawPathGizmos()
    {
        if (_agent == null || _agent.hasPath == false) return;

        // 경로 선 (녹색)
        Gizmos.color = Color.green;
        Vector3[] corners = _agent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
        }

        // 목적지 (노란 원)
        if (corners.Length > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(corners[corners.Length - 1], 0.5f);
        }

        // 속도 벡터 (파란 선)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + _agent.velocity);
    }
#endif
}
