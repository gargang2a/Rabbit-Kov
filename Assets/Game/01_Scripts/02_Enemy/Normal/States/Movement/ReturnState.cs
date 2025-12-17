using UnityEngine;

// [역할] 복귀 상태 - 타겟 소실 후 현재 위치 기준 랜덤 이동
public class ReturnState : IMovementState
{
    private bool _hasStartedPatrol = false;
    private bool _shouldFallbackToPatrol = false;
    
    /// <summary>
    /// 상태 진입: 현재 위치 기준 랜덤 정찰 시작
    /// </summary>
    public void Enter(EnemyController enemy)
    {
        _hasStartedPatrol = false;
        _shouldFallbackToPatrol = false;
        
        // 현재 위치에서 랜덤 정찰 시작
        if (enemy.Movement != null)
        {
            enemy.Movement.SetPatrolSpeed();
            _hasStartedPatrol = enemy.Movement.StartRandomPatrol();
            
            if (!_hasStartedPatrol)
            {
                // 랜덤 정찰 실패 시 다음 프레임에 PatrolState로 전환
                _shouldFallbackToPatrol = true;
                Debug.Log($"{enemy.gameObject.name}: ReturnState - 랜덤 위치 탐색 실패, PatrolState로 전환 예정");
            }
        }
        else
        {
            _shouldFallbackToPatrol = true;
        }
        
        Debug.Log($"{enemy.gameObject.name}: ReturnState 진입");
    }

    /// <summary>
    /// 매 프레임 실행
    /// </summary>
    public void Execute(EnemyController enemy)
    {
        // 복귀 중 플레이어 감지 시 추적으로 전환
        if (enemy.HasTarget())
        {
            enemy.ChangeMovementState(enemy.ChaseMovementState);
            return;
        }
        
        // 폴백: 랜덤 정찰 실패 시 PatrolState로 전환
        if (_shouldFallbackToPatrol)
        {
            enemy.ChangeMovementState(enemy.PatrolMovementState);
            return;
        }
        
        // 목적지 도착 체크
        if (enemy.Movement != null && enemy.Movement.HasReachedDestination)
        {
            // 도착 → 정찰 상태로 전환
            enemy.ChangeMovementState(enemy.PatrolMovementState);
        }
    }

    /// <summary>
    /// 상태 종료
    /// </summary>
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: ReturnState 종료");
    }
}

