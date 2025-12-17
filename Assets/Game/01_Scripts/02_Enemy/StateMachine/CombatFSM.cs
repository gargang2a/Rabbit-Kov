using UnityEngine;

// [역할] 전투 FSM - 전투 관련 상태 전환 및 실행 관리
// Inactive, Ready, Windup, Attacking, Recovery 상태를 관리
public class CombatFSM
{
    private ICombatState _currentState;
    private string _currentStateName = "None";
    
    // 프로퍼티
    public string CurrentStateName => _currentStateName;
    public ICombatState CurrentState => _currentState;
    public bool IsActive => !(_currentState is CombatInactiveState);

    /// <summary>
    /// 상태 전환: Exit → 새 상태 저장 → Enter
    /// </summary>
    public void ChangeState(ICombatState newState, EnemyController enemy)
    {
        // 이전 상태의 이동 잠금 해제
        if (_currentState != null && _currentState.RequiresMovementLock)
        {
            enemy.UnlockMovement();
        }
        
        _currentState?.Exit(enemy);
        _currentState = newState;

        if (_currentState != null)
        {
            _currentState.Enter(enemy);
            _currentStateName = _currentState.GetType().Name;
            
            // 새 상태가 이동 잠금을 요구하면 적용
            if (_currentState.RequiresMovementLock)
            {
                enemy.LockMovement();
            }
        }
        else
        {
            _currentStateName = "None";
        }
    }

    /// <summary>
    /// 매 프레임 현재 상태 실행
    /// </summary>
    public void Update(EnemyController enemy)
    {
        _currentState?.Execute(enemy);
    }
    
    /// <summary>
    /// 전투 FSM 리셋 (타겟 소실 등)
    /// </summary>
    public void Reset(EnemyController enemy, ICombatState inactiveState)
    {
        if (_currentState != null && _currentState.RequiresMovementLock)
        {
            enemy.UnlockMovement();
        }
        
        _currentState?.Exit(enemy);
        _currentState = inactiveState;
        _currentState?.Enter(enemy);
        _currentStateName = "CombatInactiveState";
    }
}
