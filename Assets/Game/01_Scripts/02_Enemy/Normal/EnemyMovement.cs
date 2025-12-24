using UnityEngine;
using UnityEngine.AI;

// [역할] 적 이동 시스템 - NavMesh 기반 이동 및 회전
public class EnemyMovement : MonoBehaviour
{
    [Header("Fallback Settings (DataSO Overrides)")]
    [SerializeField] private float _patrolSpeed = 3.5f;       // 정찰 속도 (m/s)
    [SerializeField] private float _chaseSpeed = 5.5f;        // 추격 속도 (m/s)
    [SerializeField] private float _rotateSpeed = 120f;       // 회전 속도 (도/초)

    [Header("정찰 설정")]
    [Tooltip("최소 정찰 이동 거리 (m)")]
    [SerializeField] private float _minPatrolDistance = 2f;   // 최소 정찰 거리
    [Tooltip("최대 정찰 이동 거리 (m)")]
    [SerializeField] private float _maxPatrolDistance = 20f;  // 최대 정찰 거리
    [SerializeField] private bool _useRandomPatrol = true;    // 랜덤 정찰 사용

    [Header("도착 판정")]
    [Tooltip("목적지까지 이 거리 이내면 도착 판정 (m)")]
    [SerializeField] private float _arrivalThreshold = 0.5f;  // 도착 임계값

    [Header("적 분리 (Separation)")]
    [Tooltip("다른 적과의 최소 거리 - 이보다 가까우면 밀어냄")]
    [SerializeField] private float _separationDistance = 1.5f;  // 분리 거리
    [Tooltip("분리 힘 강도 (0~1) - 높으면 떨림 발생 가능")]
    [SerializeField] private float _separationStrength = 0.2f;  // 분리 강도 (0.5 → 0.2 완화)
    [Tooltip("NavMeshAgent 회피 반경 (Normal만 적용)")]
    [SerializeField] private float _avoidanceRadius = 0.5f;     // 회피 반경

    private NavMeshAgent _agent;           // NavMesh 에이전트
    private EnemyController _controller;   // 컨트롤러 참조
    private Collider[] _boundZones;        // 이동 제한 Zone

    // GC 방지용 캐싱
    private NavMeshPath _cachedPath;                                      // 경로 캐싱
    private static readonly Collider[] _separationBuffer = new Collider[32]; // 분리 버퍼

    // 프로퍼티
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
            if (_agent == null) return false;                                      // 에이전트 없으면 false
            if (_agent.pathPending) return false;                                  // 경로 계산 중
            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid) return false;  // 경로 실패

            float effectiveArrivalDist = Mathf.Max(_agent.stoppingDistance, _arrivalThreshold); // 유효 도착 거리

            if (_agent.hasPath == false)                          // 경로 없으면
            {
                return _agent.velocity.sqrMagnitude < 0.01f;      // 정지 상태 = 도착
            }

            if (_agent.pathStatus == NavMeshPathStatus.PathPartial) // 부분 경로
            {
                if (_agent.velocity.sqrMagnitude < 0.01f) return true; // 정지 = 도착
            }

            if (_agent.remainingDistance <= effectiveArrivalDist) return true; // 거리 내 = 도착
            if (_agent.velocity.sqrMagnitude < 0.01f && _agent.remainingDistance < 1f) return true; // 정지 + 가까움 = 도착

            return false;
        }
    }

    public bool IsMoving => _agent.velocity.sqrMagnitude > 0.01f; // 이동 중 여부

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();       // 에이전트 캐싱
        _controller = GetComponent<EnemyController>(); // 컨트롤러 캐싱
        _cachedPath = new NavMeshPath();             // 경로 캐싱
    }

    private void Start()
    {
        EnsureOnNavMesh(); // NavMesh 위 보정

        _agent.updatePosition = true; // 위치 자동 갱신
        _agent.updateRotation = true; // 회전 자동 갱신

        if (_controller?.EnemyData != null) // DataSO가 있으면
        {
            Initialize(_controller.EnemyData); // 초기화
        }

        // Normal: 약한 회피 적용
        if (_controller != null && !_controller.RestrictToZone)
        {
            _agent.radius = _avoidanceRadius;                                         // 반경 설정
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // 회피 품질
        }
    }

    // EnemyDataSO 기반 초기화
    public void Initialize(EnemyDataSO data)
    {
        _patrolSpeed = data.moveSpeed;                           // 정찰 속도
        _chaseSpeed = data.chaseSpeed;                           // 추격 속도
        _rotateSpeed = data.rotationSpeed * 12f;                 // 회전 속도 스케일
        _maxPatrolDistance = data.patrolRadius;                  // 최대 정찰 거리
        _minPatrolDistance = Mathf.Min(2f, data.patrolRadius * 0.2f); // 최소 정찰 거리

        if (_agent != null)
        {
            _agent.speed = _patrolSpeed;       // 에이전트 속도
            _agent.angularSpeed = _rotateSpeed; // 에이전트 회전
        }
    }

    // 분리 로직 스로틀링용
    private float _separationTimer = 0f;
    private const float SEPARATION_INTERVAL = 0.1f; // 0.1초마다 계산
    private Vector3 _targetSeparation = Vector3.zero; // 목표 분리량
    private Vector3 _currentSeparation = Vector3.zero; // 현재 분리량 (보간용)

    // Normal 분리 로직 (스로틀링 적용)
    // [수정] _agent.Move()로 인한 떨림 방지를 위해 비활성화
    // 분리는 MoveTo()의 ApplySeparation()에서 목적지 오프셋으로만 처리
    private void Update()
    {
        // [Opt] 분리 로직 비활성화 - NavMesh 회피만 사용
        // 떨림이 해결되면 이 코드 블록 삭제 가능
        /*
        if (_controller != null && !_controller.RestrictToZone && _agent != null && _agent.isOnNavMesh)
        {
            _separationTimer += Time.deltaTime;
            if (_separationTimer >= SEPARATION_INTERVAL)
            {
                _separationTimer = 0f;
                CalculateSeparation();
            }
            ApplySmoothSeparation();
        }
        */
    }

    // 분리량 계산 (스로틀링됨)
    private void CalculateSeparation()
    {
        Vector3 separationMove = Vector3.zero;
        float triggerDistance = _separationDistance * 0.8f;

        int count = Physics.OverlapSphereNonAlloc(transform.position, _separationDistance, _separationBuffer);

        for (int i = 0; i < count; i++)
        {
            Collider col = _separationBuffer[i];
            if (col.gameObject == gameObject) continue;

            EnemyController otherEnemy = col.GetComponent<EnemyController>();
            if (otherEnemy == null) continue;

            Vector3 diff = transform.position - col.transform.position;
            float distance = diff.magnitude;

            if (distance < triggerDistance && distance > 0.01f)
            {
                float ratio = 1f - (distance / triggerDistance);
                float pushStrength = ratio * _separationStrength;
                separationMove += diff.normalized * pushStrength;
            }
        }

        _targetSeparation = separationMove; // 목표 분리량 저장
    }

    // 부드러운 분리 적용 (비활성화됨)
    // [수정] _agent.Move()가 NavMeshAgent 경로와 충돌하여 떨림 발생
    // 분리는 MoveTo()의 목적지 오프셋으로만 처리
    private void ApplySmoothSeparation()
    {
        // [Disabled] 떨림 방지를 위해 비활성화
        // NavMeshAgent의 obstacleAvoidanceType으로 대체
        /*
        _currentSeparation = Vector3.Lerp(_currentSeparation, _targetSeparation, Time.deltaTime * 5f);
        
        if (_currentSeparation.sqrMagnitude > 0.0001f)
        {
            Vector3 moveOffset = _currentSeparation * Time.deltaTime * 2f;
            _agent.Move(moveOffset); // ← 이 부분이 떨림 원인!
        }
        */
    }

    // Zone 설정 (복수)
    public void SetBoundZones(Collider[] zones) => _boundZones = zones;

    // Zone 설정 (단일)
    public void SetBoundZone(Collider zone)
    {
        if (zone != null)
        {
            _boundZones = new Collider[] { zone }; // Zone 배열 생성
        }
        else
        {
            _boundZones = null; // Zone 없음
        }
    }

    // Zone 내부 여부 (콜라이더 타입별 정확한 검증)
    private bool IsInsideZone(Vector3 position)
    {
        if (_boundZones == null || _boundZones.Length == 0) return true; // Zone 없으면 제한 없음

        foreach (var zone in _boundZones)
        {
            if (zone == null) continue;

            // Y 좌표 무시하고 XZ 평면에서 검증
            Vector3 checkPoint = new Vector3(position.x, zone.bounds.center.y, position.z);

            // BoxCollider
            if (zone is BoxCollider box)
            {
                Vector3 localPoint = box.transform.InverseTransformPoint(checkPoint);
                Vector3 halfSize = box.size * 0.5f;
                Vector3 offset = localPoint - box.center;

                if (Mathf.Abs(offset.x) <= halfSize.x && Mathf.Abs(offset.z) <= halfSize.z)
                    return true;
            }
            // SphereCollider
            else if (zone is SphereCollider sphere)
            {
                Vector3 worldCenter = sphere.transform.TransformPoint(sphere.center);
                float radiusWorld = sphere.radius * Mathf.Max(sphere.transform.lossyScale.x, sphere.transform.lossyScale.z);
                float distXZ = Vector2.Distance(new Vector2(checkPoint.x, checkPoint.z), new Vector2(worldCenter.x, worldCenter.z));

                if (distXZ <= radiusWorld)
                    return true;
            }
            // CapsuleCollider
            else if (zone is CapsuleCollider capsule)
            {
                Vector3 worldCenter = capsule.transform.TransformPoint(capsule.center);
                float radiusWorld = capsule.radius * Mathf.Max(capsule.transform.lossyScale.x, capsule.transform.lossyScale.z);
                float distXZ = Vector2.Distance(new Vector2(checkPoint.x, checkPoint.z), new Vector2(worldCenter.x, worldCenter.z));

                if (distXZ <= radiusWorld)
                    return true;
            }
            // 기타 - bounds 사용
            else if (zone.bounds.Contains(checkPoint))
            {
                return true;
            }
        }
        return false;
    }

    // 위치를 Zone 내부로 제한
    private Vector3 ClampToZone(Vector3 position)
    {
        if (_boundZones == null || _boundZones.Length == 0) return position; // Zone 없으면 그대로

        Vector3 closestPoint = position;          // 가장 가까운 점
        float closestDistance = float.MaxValue;   // 가장 가까운 거리

        foreach (var zone in _boundZones)
        {
            if (zone == null) continue;
            Vector3 point = zone.bounds.ClosestPoint(position); // 가장 가까운 점
            float distance = Vector3.Distance(position, point); // 거리
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = point;
            }
        }
        return closestPoint;
    }

    // NavMesh 위로 위치 보정
    private void EnsureOnNavMesh()
    {
        if (_agent == null || _agent.isOnNavMesh) return; // 이미 NavMesh 위면 종료

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas)) // 가까운 NavMesh 찾기
        {
            _agent.enabled = false;       // 에이전트 비활성화
            transform.position = hit.position; // 위치 보정
            _agent.enabled = true;        // 에이전트 활성화
        }
        else
        {
            Debug.LogWarning(gameObject.name + ": 근처에 NavMesh를 찾을 수 없음!");
        }
    }

    // 지정 위치로 이동
    public void MoveTo(Vector3 destination)
    {
        if (_agent == null)
        {
            Debug.LogWarning($"[MoveTo] {gameObject.name}: NavMeshAgent가 null!");
            return;
        }

        if (_agent.isOnNavMesh == false) // NavMesh 위가 아니면
        {
            Debug.LogWarning($"[MoveTo] {gameObject.name}: NavMesh 위가 아님! 보정 시도...");
            EnsureOnNavMesh(); // 보정 시도
            if (_agent.isOnNavMesh == false)
            {
                Debug.LogError($"[MoveTo] {gameObject.name}: NavMesh 보정 실패!");
                return;
            }
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 2.0f, NavMesh.AllAreas)) // 목적지 보정 (범위 축소: 5f -> 2f)
        {
            destination = hit.position;
        }

        Vector3 finalDestination = destination; // 최종 목적지

        // Normal: 분리 로직 적용
        if (_controller != null && !_controller.RestrictToZone)
        {
            finalDestination = ApplySeparation(destination); // 분리 적용
        }

        // Epic/Boss: Zone 경계 적용
        if (_controller != null && _controller.RestrictToZone && _boundZones != null && _boundZones.Length > 0 && !IsInsideZone(finalDestination))
        {
            finalDestination = ClampToZone(finalDestination); // Zone 내로 제한

            if (NavMesh.SamplePosition(finalDestination, out hit, 3f, NavMesh.AllAreas))
            {
                if (IsInsideZone(hit.position)) finalDestination = hit.position; // Zone 내 보정
                else finalDestination = transform.position; // 실패시 현재 위치
            }
        }

        _agent.isStopped = false; // 이동 시작

        _cachedPath.ClearCorners(); // 이전 경로 초기화

        if (_agent.CalculatePath(finalDestination, _cachedPath) && _cachedPath.status != NavMeshPathStatus.PathInvalid)
        {
            if (_cachedPath.status == NavMeshPathStatus.PathPartial) // 부분 경로
            {
                // Debug.LogWarning($"[MoveTo] {name}: 경로가 끊김 (Partial Path)! -> 가능한 위치까지만 이동합니다.");
            }
            _agent.SetDestination(finalDestination); // 목적지 설정
        }
        else // 경로 계산 실패
        {
            Debug.LogWarning($"[MoveTo] {name}: 1차 경로 계산 실패 -> 원본 목적지로 재시도");

            if (_agent.CalculatePath(destination, _cachedPath) && _cachedPath.status != NavMeshPathStatus.PathInvalid)
            {
                _agent.SetDestination(destination); // 원본 목적지
            }
            else
            {
                Debug.LogWarning($"[MoveTo] {name}: 2차 경로 계산 실패 -> 강제 이동 시도");
                _agent.SetDestination(finalDestination); // 강제 이동
            }
        }
    }

    // 분리 로직: 근처 적과 거리 유지
    private Vector3 ApplySeparation(Vector3 destination)
    {
        Vector3 separationForce = Vector3.zero; // 분리 힘
        int neighborCount = 0;                  // 이웃 수

        int count = Physics.OverlapSphereNonAlloc(transform.position, _separationDistance * 2f, _separationBuffer);

        for (int i = 0; i < count; i++)
        {
            Collider col = _separationBuffer[i];
            if (col.gameObject == gameObject) continue;           // 자기 자신 제외

            EnemyController otherEnemy = col.GetComponent<EnemyController>();
            if (otherEnemy == null) continue;                     // 적 아니면 스킵

            Vector3 diff = transform.position - col.transform.position;
            float distance = diff.magnitude;

            if (distance < _separationDistance && distance > 0.01f) // 분리 거리 내
            {
                float strength = 1f - (distance / _separationDistance); // 거리 비율
                separationForce += diff.normalized * strength;          // 분리 힘 누적
                neighborCount++;
            }
        }

        if (neighborCount > 0) // 이웃이 있으면
        {
            separationForce /= neighborCount;                          // 평균
            separationForce *= _separationStrength * _separationDistance;
            destination += new Vector3(separationForce.x, 0, separationForce.z); // 목적지 조정
        }

        return destination;
    }

    // 정지
    public void Stop()
    {
        if (_agent == null || _agent.isOnNavMesh == false) return;
        _agent.isStopped = true;       // 정지
        _agent.velocity = Vector3.zero; // 속도 0
    }

    // 속도 설정
    public void SetSpeed(float speed)
    {
        if (_agent != null) _agent.speed = speed;
    }

    // 정찰 속도 프리셋
    public void SetPatrolSpeed()
    {
        if (_agent == null) return;
        _agent.speed = _patrolSpeed;      // 정찰 속도
        _agent.angularSpeed = 120f;       // 기본 회전
        _agent.acceleration = 8f;         // 기본 가속
        _agent.autoBraking = true;        // 도착 시 감속
    }

    // 추격 속도 프리셋
    public void SetChaseSpeed()
    {
        if (_agent == null) return;
        _agent.speed = _chaseSpeed;        // 추격 속도
        _agent.angularSpeed = 360f;        // 빠른 회전
        _agent.acceleration = 12f;         // 가속 완화 (20 → 12, 급격한 방향전환 방지)
        _agent.autoBraking = false;        // 감속 없음
        _agent.stoppingDistance = 0.5f;    // 떨림 방지 (0.1 → 0.5)
    }

    // 타겟 방향 회전, 완료 시 true
    public bool FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized; // 방향
        if (direction.magnitude < 0.01f) return true; // 거의 같은 위치

        float angleToTarget = Vector3.SignedAngle(transform.forward, direction, Vector3.up); // 각도
        float rotateThisFrame = _rotateSpeed * Time.deltaTime; // 이번 프레임 회전량

        if (Mathf.Abs(angleToTarget) < rotateThisFrame) // 남은 각도가 작으면
        {
            rotateThisFrame = Mathf.Abs(angleToTarget); // 정확히 맞춤
        }

        float rotationDirection = Mathf.Sign(angleToTarget); // 회전 방향
        transform.Rotate(0, rotationDirection * rotateThisFrame, 0); // 회전

        return Mathf.Abs(angleToTarget) < rotateThisFrame; // 완료 여부
    }

    // 오버로드: Transform 타겟
    public bool FaceTarget(Transform target)
    {
        if (target == null) return true;
        return FaceTarget(target.position);
    }

    public bool CanPatrol() => _useRandomPatrol; // 정찰 가능 여부

    // 랜덤 정찰 시작, 성공 시 true
    public bool StartRandomPatrol()
    {
        if (_agent == null || _agent.isOnNavMesh == false) return false;

        NavMeshPath path = new NavMeshPath();

        if (_boundZones != null && _boundZones.Length > 0) // Zone이 있으면
        {
            return StartRandomPatrolInZone(path); // Zone 내 정찰
        }

        return StartRandomPatrolFree(path); // 자유 정찰
    }

    // Zone 내부 랜덤 정찰
    private bool StartRandomPatrolInZone(NavMeshPath path)
    {
        for (int attempt = 0; attempt < 20; attempt++) // 20번 시도
        {
            Collider zone = _boundZones[Random.Range(0, _boundZones.Length)]; // 랜덤 Zone
            if (zone == null) continue;

            // 콜라이더 타입별 랜덤 포인트 생성
            Vector3 targetPos = GetRandomPointInZone(zone);
            if (targetPos == Vector3.zero) continue;

            float distance = Vector3.Distance(transform.position, targetPos);
            if (distance < 1f) continue; // 너무 가까우면 스킵

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
            {
                if (!IsInsideZone(hit.position)) continue; // Zone 밖이면 스킵

                float actualDistance = Vector3.Distance(transform.position, hit.position);
                if (actualDistance < 0.5f) continue; // 너무 가까우면 스킵

                if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
                {
                    if (path.status == NavMeshPathStatus.PathComplete) // 완전한 경로
                    {
                        SetPatrolSpeed(); // 정찰 속도
                        MoveTo(hit.position); // 이동
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // Zone 내 랜덤 포인트 생성 (콜라이더 타입별)
    private Vector3 GetRandomPointInZone(Collider zone)
    {
        // BoxCollider
        if (zone is BoxCollider box)
        {
            Vector3 localPoint = new Vector3(
                Random.Range(-0.5f, 0.5f) * box.size.x,
                0,
                Random.Range(-0.5f, 0.5f) * box.size.z
            );
            return box.transform.TransformPoint(box.center + localPoint);
        }

        // SphereCollider
        if (zone is SphereCollider sphere)
        {
            Vector2 randomCircle = Random.insideUnitCircle * sphere.radius;
            Vector3 localPoint = new Vector3(randomCircle.x, 0, randomCircle.y);
            return sphere.transform.TransformPoint(sphere.center + localPoint);
        }

        // CapsuleCollider
        if (zone is CapsuleCollider capsule)
        {
            Vector2 randomCircle = Random.insideUnitCircle * capsule.radius;
            Vector3 localPoint = new Vector3(randomCircle.x, 0, randomCircle.y);
            return capsule.transform.TransformPoint(capsule.center + localPoint);
        }

        // 기타 - bounds 사용
        Bounds bounds = zone.bounds;
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            transform.position.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    // Zone 없이 자유 정찰
    private bool StartRandomPatrolFree(NavMeshPath path)
    {
        float[] distanceAttempts = { _maxPatrolDistance, _maxPatrolDistance * 0.5f, _minPatrolDistance, 3f, 1f };

        foreach (float maxDist in distanceAttempts) // 거리별 시도
        {
            float minDist = Mathf.Max(0.5f, maxDist * 0.3f);

            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector3 randomDirection = Random.onUnitSphere; // 랜덤 방향
                randomDirection.y = 0;
                float randomDistance = Random.Range(minDist, maxDist); // 랜덤 거리
                Vector3 targetPos = transform.position + randomDirection * randomDistance;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPos, out hit, maxDist, NavMesh.AllAreas))
                {
                    float actualDistance = Vector3.Distance(transform.position, hit.position);
                    if (actualDistance < 0.3f) continue; // 너무 가까움

                    if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            SetPatrolSpeed();
                            MoveTo(hit.position);
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        DrawPatrolRadiusGizmos(); // 정찰 범위
        DrawPathGizmos();         // 경로
    }

    // 정찰 범위 시각화
    private void DrawPatrolRadiusGizmos()
    {
        if (!_useRandomPatrol) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // 시안색
        Gizmos.DrawWireSphere(transform.position, _maxPatrolDistance); // 최대 범위

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // 노란색
        Gizmos.DrawWireSphere(transform.position, _minPatrolDistance); // 최소 범위
    }

    // 경로 시각화
    private void DrawPathGizmos()
    {
        if (_agent == null || _agent.hasPath == false) return;

        Gizmos.color = Color.green; // 녹색
        Vector3[] corners = _agent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i + 1]); // 경로 선
        }

        if (corners.Length > 0)
        {
            Gizmos.color = Color.yellow; // 노란색
            Gizmos.DrawWireSphere(corners[corners.Length - 1], 0.5f); // 목적지
        }

        Gizmos.color = Color.blue; // 파란색
        Gizmos.DrawLine(transform.position, transform.position + _agent.velocity); // 속도 벡터
    }
#endif
}
