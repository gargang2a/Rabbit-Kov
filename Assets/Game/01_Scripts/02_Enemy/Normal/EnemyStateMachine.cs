using UnityEngine;

// [역할] 적 AI 상태 기계 (레거시 FSM) - 상태 전환 및 실행 관리
public class EnemyStateMachine
{
    private IEnemyState _currentState;            // 현재 상태
    private string _currentStateName = "None";    // 상태 이름 (디버깅용)

    public string CurrentStateName => _currentStateName; // 현재 상태 이름

    // 상태 전환
    public void ChangeState(IEnemyState newState, EnemyController enemy)
    {
        _currentState?.Exit(enemy);  // 기존 상태 종료
        _currentState = newState;    // 새 상태 저장

        if (_currentState != null)
        {
            _currentState.Enter(enemy);                    // 새 상태 진입
            _currentStateName = _currentState.GetType().Name; // 이름 갱신
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
}
