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

    [Header("적 분리 (Separation)")]
    [Tooltip("다른 적과의 최소 거리 (m) - 이보다 가까우면 밀어냄")]
    [SerializeField] private float _separationDistance = 1.5f;  // 분리 거리
    [Tooltip("분리 힘의 강도 (0~1)")]
    [SerializeField] private float _separationStrength = 0.5f; // 분리 강도
    [Tooltip("NavMeshAgent 회피 반경 (NormEnemy만 적용)")]
    [SerializeField] private float _avoidanceRadius = 0.5f; // 회피 반경

    private NavMeshAgent _agent;   // NavMesh 에이전트
    private EnemyController _controller; // 컨트롤러 참조
    private Collider[] _boundZones;   // 이동 제한 Zone들 (복수)

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
        _controller = GetComponent<EnemyController>();
    }

    private void Start()
    {
        EnsureOnNavMesh(); // NavMesh 위 보정
        
        // NavMeshAgent 설정 강제 적용
        _agent.updatePosition = true;
        _agent.updateRotation = true;
        
        // 일반 몬스터: 약간의 회피 적용
        var controller = GetComponent<EnemyController>();
        if (controller != null && !controller.IsEpic)
        {
            // NavMeshAgent 반경을 Inspector 설정값으로 적용
            _agent.radius = _avoidanceRadius;
            _agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }
    }
    
    // 매 프레임 분리 로직 실행 (NormEnemy만)
    private void Update()
    {
        if (_controller != null && !_controller.IsEpic && _agent != null && _agent.isOnNavMesh)
        {
            ApplySoftSeparation();
        }
    }
    
    // 분리 로직: 약간의 겹침은 허용하되 완전 겹침 방지
    private void ApplySoftSeparation()
    {
        Vector3 separationMove = Vector3.zero;
        float triggerDistance = _separationDistance * 0.8f;
        
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, _separationDistance);
        
        foreach (Collider col in nearbyEnemies)
        {
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
        
        // 분리 이동 적용
        if (separationMove.sqrMagnitude > 0.001f)
        {
            Vector3 moveOffset = separationMove * Time.deltaTime * 3f;
            _agent.Move(moveOffset);
        }
    }

    // Zone 할당 (정찰 범위 제한용) - 복수 Zone 지원
    public void SetBoundZones(Collider[] zones)
    {
        _boundZones = zones;
    }
    
    // 단일 Zone 할당 (하위 호환용)
    public void SetBoundZone(Collider zone)
    {
        _boundZones = zone != null ? new Collider[] { zone } : null;
    }

    // Zone 내부 여부 확인 (어느 Zone이든 내부면 true)
    private bool IsInsideZone(Vector3 position)
    {
        if (_boundZones == null || _boundZones.Length == 0) return true; // Zone 없으면 제한 없음
        
        foreach (var zone in _boundZones)
        {
            if (zone != null && zone.bounds.Contains(position))
                return true;
        }
        return false;
    }

    // 위치를 가장 가까운 Zone 내부로 제한
    private Vector3 ClampToZone(Vector3 position)
    {
        if (_boundZones == null || _boundZones.Length == 0) return position;
        
        Vector3 closestPoint = position;
        float closestDistance = float.MaxValue;
        
        foreach (var zone in _boundZones)
        {
            if (zone == null) continue;
            Vector3 point = zone.bounds.ClosestPoint(position);
            float distance = Vector3.Distance(position, point);
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
        if (_agent == null)
        {
            Debug.LogWarning($"[MoveTo] {gameObject.name}: NavMeshAgent가 null!");
            return;
        }

        // NavMesh 위가 아니면 보정
        if (_agent.isOnNavMesh == false)
        {
            Debug.LogWarning($"[MoveTo] {gameObject.name}: NavMesh 위가 아님! 보정 시도...");
            EnsureOnNavMesh();
            if (_agent.isOnNavMesh == false)
            {
                Debug.LogError($"[MoveTo] {gameObject.name}: NavMesh 보정 실패!");
                return;
            }
        }

        // Zone 경계 적용 - Epic 몬스터만 Zone 내부로 제한
        if (_controller != null && _controller.IsEpic && _boundZones != null && _boundZones.Length > 0 && !IsInsideZone(destination))
        {
            destination = ClampToZone(destination);
        }

        // 목적지를 NavMesh 위로 보정
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 5f, NavMesh.AllAreas))
        {
            destination = hit.position;
        }

        // NormEnemy 분리 로직 - 다른 적과 너무 가까우면 약간 엇으로 이동
        if (_controller != null && !_controller.IsEpic)
        {
            destination = ApplySeparation(destination);
        }

        _agent.isStopped = false;
        _agent.SetDestination(destination);
    }

    // 분리 로직: 근처 적들과 거리를 유지하도록 목적지 조정
    private Vector3 ApplySeparation(Vector3 destination)
    {
        Vector3 separationForce = Vector3.zero;
        int neighborCount = 0;
        
        // 근처 적 찾기
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, _separationDistance * 2f);
        
        foreach (Collider col in nearbyEnemies)
        {
            // 자기 자신 제외
            if (col.gameObject == gameObject) continue;
            
            // 적인지 확인
            EnemyController otherEnemy = col.GetComponent<EnemyController>();
            if (otherEnemy == null) continue;
            
            Vector3 diff = transform.position - col.transform.position;
            float distance = diff.magnitude;
            
            // 분리 거리 내에 있으면 밀어내는 힘 적용
            if (distance < _separationDistance && distance > 0.01f)
            {
                // 거리가 가까울수록 강한 힘
                float strength = 1f - (distance / _separationDistance);
                separationForce += diff.normalized * strength;
                neighborCount++;
            }
        }
        
        // 분리 힘 적용
        if (neighborCount > 0)
        {
            separationForce /= neighborCount;
            separationForce *= _separationStrength * _separationDistance;
            destination += new Vector3(separationForce.x, 0, separationForce.z);
        }
        
        return destination;
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
    public void SetChaseSpeed() => _agent.speed = _chaseSpeed; // 추격 속도

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
        if (_agent == null || _agent.isOnNavMesh == false) 
        {
            Debug.LogWarning($"{gameObject.name}: StartRandomPatrol 실패 - NavMesh 위에 없음");
            return false;
        }

        NavMeshPath path = new NavMeshPath();
        
        // 거리 폴백: 최대 거리부터 점점 줄여가며 시도
        float[] distanceAttempts = { _maxPatrolDistance, _maxPatrolDistance * 0.5f, _minPatrolDistance };
        
        foreach (float maxDist in distanceAttempts)
        {
            float minDist = Mathf.Min(_minPatrolDistance, maxDist * 0.5f);
            
            for (int attempt = 0; attempt < 10; attempt++)
            {
                // 랜덤 방향 및 거리 생성
                Vector3 randomDirection = Random.onUnitSphere;
                randomDirection.y = 0; // 수평 방향만
                float randomDistance = Random.Range(minDist, maxDist);
                Vector3 targetPos = transform.position + randomDirection * randomDistance;

                // Zone 체크 (BoundZones가 null이면 통과)
                if (!IsInsideZone(targetPos)) continue;

                // NavMesh 위 유효 위치 탐색
                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPos, out hit, maxDist, NavMesh.AllAreas))
                {
                    if (!IsInsideZone(hit.position)) continue;

                    float actualDistance = Vector3.Distance(transform.position, hit.position);
                    if (actualDistance < 0.5f) continue; // 너무 가까우면 재시도

                    // 경로 유효성 검증
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

        Debug.LogWarning($"{gameObject.name}: StartRandomPatrol 실패 - 유효 경로 없음");
        return false;
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
