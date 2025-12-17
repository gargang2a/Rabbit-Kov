using UnityEngine;

// [역할] 이동 FSM - 이동 관련 상태 전환 및 실행 관리
// Patrol, Chase, Stopped, Return 상태를 관리
public class MovementFSM
{
    private IMovementState _currentState;
    private string _currentStateName = "None";
    
    // 이동 잠금 상태 (전투 FSM에서 정지 공격 시 요청)
    private bool _isLocked = false;
    private IMovementState _stateBeforeLock;
    
    // 프로퍼티
    public string CurrentStateName => _currentStateName;
    public bool IsLocked => _isLocked;
    public IMovementState CurrentState => _currentState;

    /// <summary>
    /// 상태 전환: Exit → 새 상태 저장 → Enter
    /// </summary>
    public void ChangeState(IMovementState newState, EnemyController enemy)
    {
        // 잠금 상태에서는 상태 전환 차단 (StoppedState로의 전환은 예외)
        if (_isLocked && !(newState is StoppedState))
        {
            return;
        }
        
        _currentState?.Exit(enemy);
        _currentState = newState;

        if (_currentState != null)
        {
            _currentState.Enter(enemy);
            _currentStateName = _currentState.GetType().Name;
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
    /// 이동 잠금 (전투 FSM에서 정지 공격 시 호출)
    /// 현재 상태를 저장하고 Stopped 상태로 강제 전환
    /// </summary>
    public void Lock(EnemyController enemy, IMovementState stoppedState)
    {
        if (_isLocked) return;
        
        _isLocked = true;
        _stateBeforeLock = _currentState;
        
        // Stopped 상태로 강제 전환
        _currentState?.Exit(enemy);
        _currentState = stoppedState;
        _currentState?.Enter(enemy);
        _currentStateName = "StoppedState";
    }
    
    /// <summary>
    /// 이동 잠금 해제 (전투 FSM에서 공격 완료 시 호출)
    /// 이전 상태로 복귀
    /// </summary>
    public void Unlock(EnemyController enemy)
    {
        if (!_isLocked) return;
        
        _isLocked = false;
        
        // 이전 상태로 복귀
        if (_stateBeforeLock != null)
        {
            _currentState?.Exit(enemy);
            _currentState = _stateBeforeLock;
            _currentState?.Enter(enemy);
            _currentStateName = _currentState.GetType().Name;
            _stateBeforeLock = null;
        }
    }
}
