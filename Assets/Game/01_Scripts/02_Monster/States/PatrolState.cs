using UnityEngine;

// 순찰 상태 - 랜덤 이동 후 주변 둘러보기 반복
public class PatrolState : IEnemyState
{
    private float _lookAroundTimer = 0f;              // 둘러보기 경과 시간
    private float _lookAroundDuration = 3f;           // 둘러보기 총 시간 (초)
    
    private bool _hasReachedLookDirection = true;     // 목표 방향 회전 완료 여부
    private Vector3 _currentLookTarget;               // 현재 바라볼 목표 위치
    private float _lookChangeInterval = 0.8f;         // 방향 변경 간격 (초)
    private float _lastLookChangeTime = 0f;           // 마지막 방향 변경 시간
    
    private bool _isLookingAround = false;            // 둘러보기 모드 여부
    private float _moveStartTime = 0f;                // 이동 시작 시간
    private float _minMoveTime = 0.5f;                // 최소 이동 시간 (초)

    // 상태 진입: 타이머 초기화, 랜덤 이동 시작
    public void Enter(EnemyController enemy)
    {
        _isLookingAround = false;
        _lookAroundTimer = 0f;
        _hasReachedLookDirection = true;
        _moveStartTime = Time.time;

        // 랜덤 정찰 시작
        if (enemy.Movement?.UseRandomPatrol == true)
        {
            enemy.Movement.StartRandomPatrol();
        }

        Debug.Log(enemy.gameObject.name + ": PatrolState 진입");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // 플레이어 감지 시 상태 전환
        if (enemy.HasTarget())
        {
            // 에픽: Chase로 전환, 일반: Rush로 전환
            if (enemy.IsEpic)
                enemy.ChangeToChase();
            else
                enemy.ChangeToRush();
            return;
        }

        if (enemy.Movement == null) return;

        // === 둘러보기 모드 (도착 후 3초간) ===
        if (_isLookingAround)
        {
            _lookAroundTimer += Time.deltaTime;

            // 일정 간격마다 다른 방향 바라보기
            if (Time.time - _lastLookChangeTime >= _lookChangeInterval)
            {
                SetRandomLookDirection(enemy);
                _lastLookChangeTime = Time.time;
                _hasReachedLookDirection = false;
            }

            // 목표 방향으로 부드럽게 회전
            if (!_hasReachedLookDirection)
            {
                _hasReachedLookDirection = enemy.Movement.FaceTarget(_currentLookTarget);
            }

            // 둘러보기 완료 → 다음 순찰 지점으로 이동
            if (_lookAroundTimer >= _lookAroundDuration)
            {
                _isLookingAround = false;
                _lookAroundTimer = 0f;
                _moveStartTime = Time.time;

                if (enemy.Movement.UseRandomPatrol)
                {
                    enemy.Movement.StartRandomPatrol();
                }
            }
            return;
        }

        // === 이동 중 ===
        // 최소 이동 시간 체크 (시작 직후 오판정 방지)
        if (Time.time - _moveStartTime < _minMoveTime) return;

        // 목적지 도착 시 둘러보기 모드로 전환
        if (enemy.Movement.HasReachedDestination)
        {
            _isLookingAround = true;
            _lookAroundTimer = 0f;
            _lastLookChangeTime = Time.time;
            enemy.Movement.Stop();
            SetRandomLookDirection(enemy);
            _hasReachedLookDirection = false;
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log(enemy.gameObject.name + ": PatrolState 종료");
    }

    // 랜덤한 방향 생성 (좌우 120도 범위)
    private void SetRandomLookDirection(EnemyController enemy)
    {
        float randomAngle = Random.Range(-120f, 120f);
        Vector3 randomDirection = Quaternion.Euler(0, randomAngle, 0) * enemy.transform.forward;
        _currentLookTarget = enemy.transform.position + randomDirection * 5f;
    }
}
