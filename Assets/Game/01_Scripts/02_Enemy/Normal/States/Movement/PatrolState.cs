using UnityEngine;

// [역할] 순찰 상태 - 랜덤 이동 후 주변 둘러보기 반복
public class PatrolState : IMovementState
{
    private float _lookAroundTimer = 0f;          // 둘러보기 타이머
    private float _lookAroundDuration = 3f;       // 둘러보기 시간
    
    private bool _hasReachedLookDirection = true; // 회전 완료 여부
    private Vector3 _currentLookTarget;           // 현재 바라볼 방향
    private float _lookChangeInterval = 0.8f;     // 방향 변경 간격
    private float _lastLookChangeTime = 0f;       // 마지막 방향 변경 시간
    
    private bool _isLookingAround = false;        // 둘러보기 모드 여부
    private float _moveStartTime = 0f;            // 이동 시작 시간
    private float _minMoveTime = 0.5f;            // 최소 이동 시간
    
    // 이동 타임아웃
    private float _moveTimeout = 5f;              // 5초 후 새 위치 탐색
    private int _failedPatrolAttempts = 0;        // 실패 횟수
    private int _maxFailedAttempts = 3;           // 최대 실패 횟수
    
    // 멈춤 감지
    private float _stuckCheckTime = 0f;           // 멈춤 체크 타이머
    private float _stuckThreshold = 2f;           // 2초간 이동 없으면 멈춤 판정

    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        _isLookingAround = false;
        _lookAroundTimer = 0f;
        _hasReachedLookDirection = true;
        _moveStartTime = Time.time;
        _failedPatrolAttempts = 0;
        _stuckCheckTime = 0f;

        if (StartNewPatrol(enemy)) // 정찰 시작 실패 시
        {
            _isLookingAround = true; // 즉시 둘러보기 모드
        }

        Debug.Log($"{enemy.gameObject.name}: PatrolState 진입");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // 타겟 감지 시 추적
        if (enemy.HasTarget())
        {
            enemy.ChangeMovementState(enemy.ChaseMovementState);
            return;
        }

        if (enemy.Movement == null) return;

        // 둘러보기 모드
        if (_isLookingAround)
        {
            _lookAroundTimer += Time.deltaTime;

            if (Time.time - _lastLookChangeTime >= _lookChangeInterval) // 방향 변경 시간 도래
            {
                SetRandomLookDirection(enemy);
                _lastLookChangeTime = Time.time;
                _hasReachedLookDirection = false;
            }

            if (!_hasReachedLookDirection) // 회전 중
            {
                _hasReachedLookDirection = enemy.Movement.FaceTarget(_currentLookTarget);
            }

            if (_lookAroundTimer >= _lookAroundDuration) // 둘러보기 완료
            {
                _isLookingAround = false;
                _lookAroundTimer = 0f;
                _moveStartTime = Time.time;
                _stuckCheckTime = 0f;
                _failedPatrolAttempts = 0;

                if (StartNewPatrol(enemy)) // 정찰 실패 시
                {
                    _isLookingAround = true; // 계속 둘러보기
                }
            }
            return;
        }

        // 이동 중
        if (Time.time - _moveStartTime < _minMoveTime) return; // 최소 이동 시간

        if (enemy.Movement.HasReachedDestination) // 도착
        {
            OnPatrolSuccess(enemy);
            return;
        }
        
        // 멈춤 감지
        if (!enemy.Movement.IsMoving)
        {
            _stuckCheckTime += Time.deltaTime;
            if (_stuckCheckTime >= _stuckThreshold) // 멈춤 판정
            {
                HandlePatrolFailed(enemy, "멈춤 감지");
                return;
            }
        }
        else
        {
            _stuckCheckTime = 0f; // 이동 중이면 리셋
        }
        
        // 타임아웃
        if (Time.time - _moveStartTime >= _moveTimeout)
        {
            HandlePatrolFailed(enemy, "타임아웃");
        }
    }
    
    // 정찰 실패 처리
    private void HandlePatrolFailed(EnemyController enemy, string reason)
    {
        _failedPatrolAttempts++;
        
        if (_failedPatrolAttempts >= _maxFailedAttempts) // 여러 번 실패
        {
            OnPatrolSuccess(enemy); // 제자리 둘러보기
        }
        else
        {
            _moveStartTime = Time.time;
            _stuckCheckTime = 0f;
            if (StartNewPatrol(enemy)) // 재시도 실패
            {
                OnPatrolSuccess(enemy);
            }
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: PatrolState 종료");
    }
    
    // 새 정찰 시작, 실패 시 true
    private bool StartNewPatrol(EnemyController enemy)
    {
        if (enemy.Movement?.UseRandomPatrol == true)
        {
            if (enemy.Movement.StartRandomPatrol())
            {
                return false; // 성공
            }
        }
        return true; // 실패
    }
    
    // 정찰 도착 → 둘러보기 모드
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

    // 랜덤 방향 생성 (좌우 120도)
    private void SetRandomLookDirection(EnemyController enemy)
    {
        float randomAngle = Random.Range(-120f, 120f);
        Vector3 randomDirection = Quaternion.Euler(0, randomAngle, 0) * enemy.transform.forward;
        _currentLookTarget = enemy.transform.position + randomDirection * 5f;
    }
}
