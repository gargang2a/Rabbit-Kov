using UnityEngine;

// [역할] 이동 FSM - 이동 상태 전환 및 실행 관리
public class MovementFSM
{
    private IMovementState _currentState;   // 현재 상태
    private string _currentStateName = "None"; // 현재 상태 이름
    
    // 이동 잠금 (전투 FSM에서 정지 공격 시)
    private bool _isLocked = false;            // 잠금 여부
    private IMovementState _stateBeforeLock;   // 잠금 전 상태
    
    // 프로퍼티
    public string CurrentStateName => _currentStateName;
    public bool IsLocked => _isLocked;
    public IMovementState CurrentState => _currentState;

    // 상태 전환
    public void ChangeState(IMovementState newState, EnemyController enemy)
    {
        // 잠금 중에는 StoppedState만 허용
        if (_isLocked && !(newState is StoppedState))
        {
            Debug.LogWarning($"[MovementFSM] {enemy.name}: 이동 잠금 상태라 상태 전환 차단됨!");
            return;
        }
        
        _currentState?.Exit(enemy); // 이전 상태 종료
        _currentState = newState;   // 새 상태 저장

        if (_currentState != null)
        {
            _currentState.Enter(enemy);                    // 새 상태 진입
            _currentStateName = _currentState.GetType().Name; // 이름 저장
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
    
    // 이동 잠금 (정지 공격 시)
    public void Lock(EnemyController enemy, IMovementState stoppedState)
    {
        if (_isLocked) return; // 이미 잠금이면 무시
        
        _isLocked = true;               // 잠금 설정
        _stateBeforeLock = _currentState; // 현재 상태 저장
        
        _currentState?.Exit(enemy);     // 현재 상태 종료
        _currentState = stoppedState;   // 정지 상태로 전환
        _currentState?.Enter(enemy);    // 정지 상태 진입
        _currentStateName = "StoppedState";
    }
    
    // 이동 잠금 해제
    public void Unlock(EnemyController enemy)
    {
        if (!_isLocked) return; // 잠금 아니면 무시
        
        _isLocked = false; // 잠금 해제
        
        if (_stateBeforeLock != null) // 이전 상태가 있으면
        {
            _currentState?.Exit(enemy);          // 현재 상태 종료
            _currentState = _stateBeforeLock;    // 이전 상태로 복원
            _currentState?.Enter(enemy);         // 이전 상태 진입
            _currentStateName = _currentState.GetType().Name;
            _stateBeforeLock = null;             // 저장 상태 초기화
        }
    }
    
    // 스턴 시스템
    private bool _isStunned = false;           // 스턴 여부
    private IMovementState _stateBeforeStun;   // 스턴 전 상태
    
    public bool IsStunned => _isStunned;
    
    // 스턴 상태로 강제 전환
    public void ForceStunState(IMovementState stunnedState, EnemyController enemy)
    {
        if (_isStunned) return; // 이미 스턴이면 무시
        
        _isStunned = true;
        
        // Lock 상태였으면 Lock 이전 상태 저장
        if (_isLocked && _stateBeforeLock != null)
        {
            _stateBeforeStun = _stateBeforeLock; // Lock 이전 상태 저장
            _isLocked = false;
            _stateBeforeLock = null;
        }
        else
        {
            _stateBeforeStun = _currentState; // 현재 상태 저장
        }
        
        _currentState?.Exit(enemy);       // 현재 상태 종료
        _currentState = stunnedState;     // 스턴 상태로 전환
        _currentState?.Enter(enemy);      // 스턴 상태 진입
        _currentStateName = "StunnedMovementState";
    }
    
    // 스턴 해제 후 복원
    public void RestoreFromStun(EnemyController enemy)
    {
        if (!_isStunned) return; // 스턴 아니면 무시
        
        _isStunned = false;
        
        if (_stateBeforeStun != null) // 이전 상태가 있으면
        {
            _currentState?.Exit(enemy);          // 현재 상태 종료
            _currentState = _stateBeforeStun;    // 이전 상태로 복원
            _currentState?.Enter(enemy);         // 이전 상태 진입
            _currentStateName = _currentState.GetType().Name;
            _stateBeforeStun = null;
        }
    }
}
