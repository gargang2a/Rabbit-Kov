using UnityEngine;

// [역할] 복귀 상태 - 타겟 소실 후 랜덤 이동
public class ReturnState : IMovementState
{
    private bool _hasStartedPatrol = false;       // 정찰 시작 여부
    private bool _shouldFallbackToPatrol = false; // 폴백 필요 여부
    
    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        _hasStartedPatrol = false;
        _shouldFallbackToPatrol = false;
        
        if (enemy.Movement != null)
        {
            enemy.Movement.SetPatrolSpeed();
            _hasStartedPatrol = enemy.Movement.StartRandomPatrol();
            
            if (!_hasStartedPatrol) // 실패 시
            {
                _shouldFallbackToPatrol = true;
                Debug.Log($"{enemy.gameObject.name}: ReturnState - 랜덤 위치 탐색 실패");
            }
        }
        else
        {
            _shouldFallbackToPatrol = true;
        }
        
        Debug.Log($"{enemy.gameObject.name}: ReturnState 진입");
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
        
        // 폴백: PatrolState로 전환
        if (_shouldFallbackToPatrol)
        {
            enemy.ChangeMovementState(enemy.PatrolMovementState);
            return;
        }
        
        // 도착 시 PatrolState로 전환
        if (enemy.Movement != null && enemy.Movement.HasReachedDestination)
        {
            enemy.ChangeMovementState(enemy.PatrolMovementState);
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: ReturnState 종료");
    }
}
