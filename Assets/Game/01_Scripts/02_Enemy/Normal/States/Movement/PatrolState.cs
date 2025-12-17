using UnityEngine;

// [역할] 순찰 상태 - 랜덤 이동 후 주변 둘러보기 반복 (병렬 FSM 버전)
// 시작 상태, 타겟 감지 시 ChaseState로 전환
public class PatrolState : IMovementState
{
    private float _lookAroundTimer = 0f;
    private float _lookAroundDuration = 3f;
    
    private bool _hasReachedLookDirection = true;
    private Vector3 _currentLookTarget;
    private float _lookChangeInterval = 0.8f;
    private float _lastLookChangeTime = 0f;
    
    private bool _isLookingAround = false;
    private float _moveStartTime = 0f;
    private float _minMoveTime = 0.5f;
    
    // 이동 타임아웃 (도달 불가 시 새 위치 탐색)
    private float _moveTimeout = 5f;  // 5초로 단축
    private int _failedPatrolAttempts = 0;
    private int _maxFailedAttempts = 3;
    
    // 멈춤 감지용
    private float _stuckCheckTime = 0f;
    private float _stuckThreshold = 2f;  // 2초간 이동 없으면 멈춤으로 판정

    /// <summary>
    /// 상태 진입: 랜덤 정찰 시작
    /// </summary>
    public void Enter(EnemyController enemy)
    {
        _isLookingAround = false;
        _lookAroundTimer = 0f;
        _hasReachedLookDirection = true;
        _moveStartTime = Time.time;
        _failedPatrolAttempts = 0;
        _stuckCheckTime = 0f;

        // 정찰 시작 실패 시 즉시 둘러보기 모드
        if (StartNewPatrol(enemy))
        {
            _isLookingAround = true;
        }

        Debug.Log($"{enemy.gameObject.name}: PatrolState 진입");
    }

    /// <summary>
    /// 매 프레임 실행
    /// </summary>
    public void Execute(EnemyController enemy)
    {
        // === 타겟 감지 시 추적 상태로 전환 ===
        if (enemy.HasTarget())
        {
            enemy.ChangeMovementState(enemy.ChaseMovementState);
            return;
        }

        if (enemy.Movement == null) return;

        // === 둘러보기 모드 ===
        if (_isLookingAround)
        {
            _lookAroundTimer += Time.deltaTime;

            if (Time.time - _lastLookChangeTime >= _lookChangeInterval)
            {
                SetRandomLookDirection(enemy);
                _lastLookChangeTime = Time.time;
                _hasReachedLookDirection = false;
            }

            if (!_hasReachedLookDirection)
            {
                _hasReachedLookDirection = enemy.Movement.FaceTarget(_currentLookTarget);
            }

            if (_lookAroundTimer >= _lookAroundDuration)
            {
                _isLookingAround = false;
                _lookAroundTimer = 0f;
                _moveStartTime = Time.time;
                _stuckCheckTime = 0f;
                _failedPatrolAttempts = 0;

                // 정찰 시작 실패 시 계속 둘러보기
                if (StartNewPatrol(enemy))
                {
                    _isLookingAround = true;
                }
            }
            return;
        }

        // === 이동 중 ===
        if (Time.time - _moveStartTime < _minMoveTime) return;

        // 도착 체크
        if (enemy.Movement.HasReachedDestination)
        {
            OnPatrolSuccess(enemy);
            return;
        }
        
        // 멈춤 감지: 이동 중인데 속도가 0이면 카운트
        if (!enemy.Movement.IsMoving)
        {
            _stuckCheckTime += Time.deltaTime;
            if (_stuckCheckTime >= _stuckThreshold)
            {
                // 멈춤 상태 → 새 위치 탐색
                HandlePatrolFailed(enemy, "멈춤 감지");
                return;
            }
        }
        else
        {
            _stuckCheckTime = 0f;  // 이동 중이면 리셋
        }
        
        // 타임아웃 체크
        if (Time.time - _moveStartTime >= _moveTimeout)
        {
            HandlePatrolFailed(enemy, "타임아웃");
        }
    }
    
    private void HandlePatrolFailed(EnemyController enemy, string reason)
    {
        _failedPatrolAttempts++;
        // Debug.Log 제거 (로그 스팸 방지)
        
        if (_failedPatrolAttempts >= _maxFailedAttempts)
        {
            // 여러 번 실패 시 제자리에서 둘러보기
            OnPatrolSuccess(enemy);
        }
        else
        {
            // 새 정찰 위치 탐색
            _moveStartTime = Time.time;
            _stuckCheckTime = 0f;
            if (StartNewPatrol(enemy))
            {
                // 재시도도 실패 시 제자리 둘러보기
                OnPatrolSuccess(enemy);
            }
        }
    }

    /// <summary>
    /// 상태 종료
    /// </summary>
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: PatrolState 종료");
    }
    
    /// <summary>
    /// 새 정찰 위치로 이동 시작. 실패 시 true 반환 (폴백 필요)
    /// </summary>
    private bool StartNewPatrol(EnemyController enemy)
    {
        if (enemy.Movement?.UseRandomPatrol == true)
        {
            if (enemy.Movement.StartRandomPatrol())
            {
                return false; // 성공
            }
        }
        return true; // 실패 - 폴백 필요
    }
    
    /// <summary>
    /// 정찰 도착 성공 → 둘러보기 모드 진입
    /// </summary>
    private void OnPatrolSuccess(EnemyController enemy)
    {
        _isLookingAround = true;
        _lookAroundTimer = 0f;
        _lastLookChangeTime = Time.time;
        _failedPatrolAttempts = 0;
        enemy.Movement?.Stop();
        SetRandomLookDirection(enemy);
        _hasReachedLookDirection = false;
    }

    /// <summary>
    /// 랜덤한 방향 생성 (좌우 120도 범위)
    /// </summary>
    private void SetRandomLookDirection(EnemyController enemy)
    {
        float randomAngle = Random.Range(-120f, 120f);
        Vector3 randomDirection = Quaternion.Euler(0, randomAngle, 0) * enemy.transform.forward;
        _currentLookTarget = enemy.transform.position + randomDirection * 5f;
    }
}

