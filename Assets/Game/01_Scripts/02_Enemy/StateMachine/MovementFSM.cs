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
            Debug.LogWarning($"[MovementFSM] {enemy.name}: 이동 잠금 상태라 상태 전환 차단됨! _isLocked={_isLocked}");
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
    
    // ========== 스턴 시스템 ==========
    
    private bool _isStunned = false;
    private IMovementState _stateBeforeStun;
    
    /// <summary>현재 스턴 상태 여부</summary>
    public bool IsStunned => _isStunned;
    
    /// <summary>
    /// 스턴 상태 강제 전환 (Lock보다 우선순위 높음)
    /// </summary>
    public void ForceStunState(IMovementState stunnedState, EnemyController enemy)
    {
        if (_isStunned) return;
        
        _isStunned = true;
        
        // Lock 상태였다면 Lock 이전 상태를 저장
        if (_isLocked && _stateBeforeLock != null)
        {
            _stateBeforeStun = _stateBeforeLock;
            _isLocked = false;
            _stateBeforeLock = null;
        }
        else
        {
            _stateBeforeStun = _currentState;
        }
        
        // 스턴 상태로 강제 전환
        _currentState?.Exit(enemy);
        _currentState = stunnedState;
        _currentState?.Enter(enemy);
        _currentStateName = "StunnedMovementState";
    }
    
    /// <summary>
    /// 스턴 해제 후 이전 상태 복원
    /// </summary>
    public void RestoreFromStun(EnemyController enemy)
    {
        if (!_isStunned) return;
        
        _isStunned = false;
        
        // 이전 상태로 복원
        if (_stateBeforeStun != null)
        {
            _currentState?.Exit(enemy);
            _currentState = _stateBeforeStun;
            _currentState?.Enter(enemy);
            _currentStateName = _currentState.GetType().Name;
            _stateBeforeStun = null;
        }
    }
}
