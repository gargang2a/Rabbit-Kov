namespace RabbitKov.Enemy
{
    // FSM(유한 상태 기계) 관리 - 한 번에 하나의 상태만 활성화됨
    public class EnemyStateMachine
    {
        private IEnemyState _currentState;        // 현재 활성화된 상태
        private string _currentStateName = "None"; // 디버깅용 상태 이름

        public string CurrentStateName { get { return _currentStateName; } }

        // 상태 변경
        public void ChangeState(IEnemyState newState, EnemyController enemy)
        {
            // 기존 상태 Exit
            if (_currentState != null)
            {
                _currentState.Exit(enemy);
            }

            _currentState = newState;

            // 새 상태 Enter
            if (_currentState != null)
            {
                _currentState.Enter(enemy);
                
                // 상태 이름 저장 (is로 타입 확인)
                if (_currentState is IdleState) _currentStateName = "IdleState";
                else if (_currentState is PatrolState) _currentStateName = "PatrolState";
                else if (_currentState is ChaseState) _currentStateName = "ChaseState";
                else if (_currentState is AttackState) _currentStateName = "AttackState";
                else if (_currentState is InvestigateState) _currentStateName = "InvestigateState";
                else _currentStateName = "Unknown";
            }
            else
            {
                _currentStateName = "None";
            }
        }

        // 매 프레임 호출 - 현재 상태의 Execute 실행
        public void Update(EnemyController enemy)
        {
            if (_currentState != null)
            {
                _currentState.Execute(enemy);
            }
        }
    }
}