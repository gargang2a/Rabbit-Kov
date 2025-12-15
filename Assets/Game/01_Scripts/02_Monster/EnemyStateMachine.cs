using UnityEngine;

// 적 AI 상태 기계 (FSM 패턴) - 상태 전환 및 실행 관리
public class EnemyStateMachine
{
    private IEnemyState _currentState;            // 현재 상태
    private string _currentStateName = "None";   // 상태 이름 (디버깅용)

    // 프로퍼티, 외부에서 읽기 전용
    public string CurrentStateName => _currentStateName;

    // 상태 전환: Exit → 새 상태 저장 → Enter
    public void ChangeState(IEnemyState newState, EnemyController enemy)
    {
        _currentState?.Exit(enemy); // 기존 상태 종료
        _currentState = newState;   // 새 상태 저장

        if (_currentState != null)
        {
            _currentState.Enter(enemy); // 새 상태 진입
            _currentStateName = _currentState.GetType().Name; // 상태 이름 갱신
        }
        else
        {
            _currentStateName = "None";
        }
    }

    // 매 프레임 현재 상태 실행
    public void Update(EnemyController enemy)
    {
        _currentState?.Execute(enemy);
    }
}
