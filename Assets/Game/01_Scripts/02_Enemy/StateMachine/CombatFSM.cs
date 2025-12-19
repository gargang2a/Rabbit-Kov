using UnityEngine;

// [역할] 전투 FSM - 전투 상태 전환 및 실행 관리
public class CombatFSM
{
    private ICombatState _currentState;        // 현재 상태
    private string _currentStateName = "None"; // 현재 상태 이름
    
    // 프로퍼티
    public string CurrentStateName => _currentStateName;
    public ICombatState CurrentState => _currentState;
    public bool IsActive => !(_currentState is CombatInactiveState); // 활성 여부

    // 상태 전환
    public void ChangeState(ICombatState newState, EnemyController enemy)
    {
        // 이전 상태가 이동 잠금 필요하면 해제
        if (_currentState != null && _currentState.RequiresMovementLock)
        {
            enemy.UnlockMovement();
        }
        
        _currentState?.Exit(enemy); // 이전 상태 종료
        _currentState = newState;   // 새 상태 저장

        if (_currentState != null)
        {
            _currentState.Enter(enemy);                    // 새 상태 진입
            _currentStateName = _currentState.GetType().Name; // 이름 저장
            
            // 새 상태가 이동 잠금 필요하면 적용
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

    // 매 프레임 실행
    public void Update(EnemyController enemy)
    {
        _currentState?.Execute(enemy); // 현재 상태 실행
    }
    
    // FSM 리셋 (타겟 소실 등)
    public void Reset(EnemyController enemy, ICombatState inactiveState)
    {
        // 이동 잠금 해제
        if (_currentState != null && _currentState.RequiresMovementLock)
        {
            enemy.UnlockMovement();
        }
        
        _currentState?.Exit(enemy);       // 현재 상태 종료
        _currentState = inactiveState;    // 비활성 상태로 전환
        _currentState?.Enter(enemy);      // 비활성 상태 진입
        _currentStateName = "CombatInactiveState";
    }
}
