using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ============================================================================
// EnemyMovement - 적 이동 시스템
// ============================================================================
// 
// [역할]
// 적의 모든 이동을 담당하는 컴포넌트입니다.
// Unity의 NavMeshAgent를 래핑하여 FSM의 각 상태에서 쉽게 이동 명령을 내릴 수 있게 합니다.
// 
// [주요 기능]
// 1. 목적지 이동 (MoveTo)
// 2. 정지 (Stop)
// 3. 속도 설정 (걷기/뛰기)
// 4. 타겟 방향으로 회전 (LookAt)
// 5. 랜덤 정찰 지점 탐색 (MoveToRandomPoint)
// 
// [NavMeshAgent란?]
// Unity의 AI 길찾기 시스템입니다.
// NavMesh(이동 가능 영역)를 미리 베이크(Bake)해두면,
// 장애물을 자동으로 피해가며 목적지까지 이동합니다.
// ============================================================================
public class EnemyMovement : MonoBehaviour
{
    // ==================== 이동 속도 설정 ====================
    
    // 걷기 속도 (m/s). PatrolState(순찰)에서 사용합니다.
    // 값이 작을수록 여유로운 순찰, 클수록 급한 순찰
    [SerializeField] private float _walkSpeed = 2f;
    
    // 뛰기 속도 (m/s). ChaseState(추적), InvestigateState(조사)에서 사용합니다.
    // 플레이어 이동속도와 비교하여 밸런스를 조절하세요.
    [SerializeField] private float _runSpeed = 5f;

    // ==================== 정찰 설정 ====================
    
    // 랜덤 정찰 반경 (m). 이 범위 내에서 랜덤한 정찰 지점을 찾습니다.
    // 값이 클수록 넓은 범위, 작을수록 좁은 범위를 순찰
    [SerializeField] private float _randomPatrolRadius = 20f;
    
    // 랜덤 정찰 사용 여부
    // true: MoveToRandomPoint()로 랜덤 이동
    // false: 고정 웨이포인트 기반 이동 (추후 확장)
    [SerializeField] private bool _useRandomPatrol = true;

    // ==================== 회전 설정 ====================
    
    // 회전 속도 (도/초). LookAt()에서 타겟을 바라볼 때 사용합니다.
    // 값이 클수록 빠르게 회전, 작을수록 부드럽게 회전
    [SerializeField] private float _rotateSpeed = 120f;

    // ==================== 컴포넌트 캐싱 ====================
    
    // NavMeshAgent 참조. Awake()에서 한 번만 GetComponent 호출
    private NavMeshAgent _agent;

    // ==================== 프로퍼티 ====================
    
    // 걷기 속도 (읽기 전용)
    public float WalkSpeed { get { return _walkSpeed; } }
    
    // 뛰기 속도 (읽기 전용)
    public float RunSpeed { get { return _runSpeed; } }
    
    // 정찰 반경 (읽기 전용)
    public float RandomPatrolRadius { get { return _randomPatrolRadius; } }
    
    // 랜덤 정찰 사용 여부 (읽기 전용)
    public bool UseRandomPatrol { get { return _useRandomPatrol; } }

    // 목적지 도착 여부
    // FSM에서 PatrolState → 대기 전환 조건으로 사용
    public bool HasReacheddestination
    {
        get
        {
            if (_agent == null) return false;
            
            // pathPending: 경로 계산 중이면 아직 도착 아님
            if (_agent.pathPending) return false;

            // 경로 상태 체크 - PathInvalid일 때만 실패
            if (_agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
            {
                return false;
            }

            // hasPath가 false = 경로 없음 (목적지 미설정 또는 이동 완료)
            if (_agent.hasPath == false)
            {
                // 속도가 거의 0이면 도착으로 판단
                return _agent.velocity.sqrMagnitude < 0.01f;
            }

            // PathPartial일 때: 부분 경로의 끝에 도달했는지 확인
            // 속도가 0이고 남은 거리가 변하지 않으면 도착으로 판정
            if (_agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial)
            {
                // 속도가 거의 0이면 부분 경로의 끝에 도달한 것
                if (_agent.velocity.sqrMagnitude < 0.01f)
                {
                    return true;
                }
            }

            // 남은 거리가 정지 거리 이하면 도착
            return _agent.remainingDistance <= _agent.stoppingDistance;
        }
    }

    // 이동 중인지 여부 (애니메이션 전환용)
    public bool isMoving
    {
        get
        {
            float speedSquared = _agent.velocity.sqrMagnitude;
            return speedSquared > 0.01f; // 속도 0.1 이상이면 이동 중
        }
    }

    // ==================== MonoBehaviour 생명주기 ====================
    
    private void Awake()
    {
        // GetComponent 캐싱: 매 프레임 호출 방지
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // NavMesh 위에 없으면 강제로 배치
        EnsureOnNavMesh();
    }

    // NavMesh 위에 있는지 확인하고, 없으면 가장 가까운 NavMesh 위치로 강제 이동
    private void EnsureOnNavMesh()
    {
        if (_agent == null) return;
        
        // 이미 NavMesh 위에 있으면 통과
        if (_agent.isOnNavMesh) return;
        
        // 가장 가까운 NavMesh 위치 찾기
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
        {
            // NavMeshAgent를 비활성화 → 위치 강제 설정 → 다시 활성화
            // 이 방법이 Warp보다 더 확실함
            _agent.enabled = false;
            transform.position = hit.position;
            _agent.enabled = true;
            
            Debug.Log(gameObject.name + ": NavMesh 위로 강제 이동 - Y전: " + transform.position.y + " Y후: " + hit.position.y);
        }
        else
        {
            Debug.LogWarning(gameObject.name + ": 근처에 NavMesh를 찾을 수 없음!");
        }
    }

    // ==================== 이동 메서드 ====================
    
    // 지정된 위치로 이동 시작
    public void MoveTo(Vector3 destination)
    {
        if (_agent == null) return;
        
        // NavMesh 위에 없으면 강제 배치
        if (_agent.isOnNavMesh == false)
        {
            EnsureOnNavMesh();
            // 여전히 NavMesh 위가 아니면 실패
            if (_agent.isOnNavMesh == false) return;
        }

        // 목적지도 NavMesh 위의 정확한 위치로 보정
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 2f, NavMesh.AllAreas))
        {
            destination = hit.position;
        }

        _agent.isStopped = false; // 이동 재개
        _agent.SetDestination(destination); // 목적지 설정 및 경로 계산
    }

    // 즉시 정지
    public void Stop()
    {
        if (_agent == null) return;
        if (_agent.isOnNavMesh == false) return;

        _agent.isStopped = true; // 이동 중지
        _agent.velocity = Vector3.zero; // 관성 제거
    }

    // ==================== 속도 설정 ====================
    
    // 속도 직접 설정
    public void SetSpeed(float speed)
    {
        if (_agent != null) _agent.speed = speed;
    }

    // 걷기 속도로 설정 (PatrolState용)
    public void SetWalkSpeed()
    {
        SetSpeed(_walkSpeed);
    }

    // 뛰기 속도로 설정 (ChaseState, InvestigateState용)
    public void SetRunSpeed()
    {
        SetSpeed(_runSpeed);
    }

    // ==================== 회전 메서드 ====================
    
    // 지정된 위치를 부드럽게 바라보기
    // 반환값: 완전히 바라보면 true, 아직 회전 중이면 false
    public bool LookAt(Vector3 target)
    {
        // 타겟 방향 계산
        Vector3 direction = (target - transform.position).normalized;
        if (direction.magnitude < 0.01f) return true; // 이미 같은 위치

        // 현재 방향과 타겟 방향의 각도 차이 (Y축 기준)
        // SignedAngle: 양수=시계방향, 음수=반시계방향
        float angleToTarget = Vector3.SignedAngle(transform.forward, direction, Vector3.up);

        // 이번 프레임 회전량
        float rotateThisFrame = _rotateSpeed * Time.deltaTime;

        // 오버슈팅 방지: 남은 각도보다 회전량이 크면 조정
        if (Mathf.Abs(angleToTarget) < rotateThisFrame)
        {
            rotateThisFrame = Mathf.Abs(angleToTarget);
        }

        // 회전 방향 결정 (-1 또는 1)
        float rotationDirection = Mathf.Sign(angleToTarget);

        // Y축 회전 적용
        transform.Rotate(0, rotationDirection * rotateThisFrame, 0);

        // 회전 완료 여부 반환
        return Mathf.Abs(angleToTarget) < rotateThisFrame;
    }

    // Transform 오버로드
    public bool LookAt(Transform target)
    {
        if (target == null) return true;
        return LookAt(target.position);
    }

    // ==================== 정찰 메서드 ====================
    
    // 정찰 가능 여부
    public bool CanPatrol()
    {
        if (_useRandomPatrol == true) return true;
        return false; // 웨이포인트 정찰은 추후 구현
    }

    // 랜덤 정찰 지점으로 이동
    // 반환값: 성공하면 true, 실패하면 false
    public bool MoveToRandomPoint()
    {
        if (_agent == null) return false;
        if (_agent.isOnNavMesh == false) return false;

        float minDistance = 5f; // 현재 위치로부터 최소 이동 거리
        float edgeOffset = 0.25f; // NavMesh 경계로부터 최소 거리
        int maxAttempts = 30; // 최대 시도 횟수

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 랜덤 방향 생성 (구 표면의 점 사용 - 중심에서 멀리)
            Vector3 randomDirection = UnityEngine.Random.onUnitSphere;
            
            // 최소~최대 범위 사이의 랜덤 거리 적용
            float randomDistance = UnityEngine.Random.Range(minDistance, _randomPatrolRadius);
            randomDirection *= randomDistance;
            
            randomDirection += transform.position; // 현재 위치 기준으로 변환
            randomDirection.y = transform.position.y; // 높이 고정

            // NavMesh 위의 유효한 위치 찾기
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 10f, NavMesh.AllAreas))
            {
                float distance = Vector3.Distance(transform.position, hit.position);
                if (distance < minDistance) continue; // 너무 가까우면 재시도

                // NavMesh 경계로부터의 거리 체크
                // FindClosestEdge: 가장 가까운 NavMesh 가장자리를 찾음
                NavMeshHit edgeHit;
                if (NavMesh.FindClosestEdge(hit.position, out edgeHit, NavMesh.AllAreas))
                {
                    // 경계로부터 너무 가까우면 재시도
                    if (edgeHit.distance < edgeOffset) continue;
                }

                // Y좌표를 현재 높이로 고정 (높이 차이로 인한 경로 문제 방지)
                Vector3 destination = hit.position;
                destination.y = transform.position.y;
                
                SetWalkSpeed();
                MoveTo(destination);
                return true;
            }
        }

        return false; // 유효한 위치를 찾지 못함
    }

    // ForceRandomMove는 MoveToRandomPoint와 동일하게 동작 (하위 호환성용)
    public bool ForceRandomMove()
    {
        return MoveToRandomPoint();
    }

// ==================== 에디터 전용: 기즈모 ====================
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        DrawPatrolGizmos();
        DrawPathGizmos();
    }

    // 정찰 범위 시각화
    private void DrawPatrolGizmos()
    {
        if (_useRandomPatrol == false) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _randomPatrolRadius);
    }

    // 이동 경로 시각화
    private void DrawPathGizmos()
    {
        if (_agent == null) return;
        if (_agent.hasPath == false) return;

        // 경로: 녹색
        Gizmos.color = Color.green;
        Vector3[] corners = _agent.path.corners;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
        }

        // 목적지: 노란색
        if (corners.Length > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(corners[corners.Length - 1], 0.5f);
        }

        // 속도 방향: 파란색
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + _agent.velocity);
    }
#endif
}
