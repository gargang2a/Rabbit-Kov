namespace RabbitKov.Enemy
{
    // FSM(유한 상태 기계)을 관리하는 클래스
    // 
    // FSM이란?
    // - 한 번에 하나의 상태만 활성화됨 (Idle, Patrol, Chase 등)
    // - 조건에 따라 다른 상태로 전환됨
    // - 각 상태는 Enter(시작), Execute(실행), Exit(종료)를 가짐
    //
    // 왜 FSM을 쓰는가?
    // - if문으로 모든 조건을 처리하면 코드가 복잡해짐 (스파게티 코드)
    // - 상태별로 코드를 분리하면 관리하기 쉬움
    // - 새 상태 추가가 간단함 (기존 코드 수정 없이)
    
    public class EnemyStateMachine
    {
        // _currentState: 현재 활성화된 상태
        // IEnemyState: 모든 상태 클래스가 구현하는 인터페이스
        // 인터페이스를 쓰면 어떤 상태든 같은 변수에 담을 수 있음
        private IEnemyState _currentState;
        
        // _currentStateName: 현재 상태의 이름 (디버깅용)
        // Inspector나 Debug.Log에서 현재 상태를 확인할 때 사용
        private string _currentStateName = "None";

        // 현재 상태 이름을 외부에서 읽을 수 있게 함
        public string CurrentStateName { get { return _currentStateName; } }

        // ChangeState: 상태를 변경하는 함수
        // newState: 새로 전환할 상태
        // enemy: 상태를 소유한 EnemyController (상태에서 컴포넌트에 접근할 때 사용)
        public void ChangeState(IEnemyState newState, EnemyController enemy)
        {
            // 기존 상태가 있으면 Exit(종료) 호출
            // Exit에서 정리 작업을 함 (예: 애니메이션 정지, 변수 초기화)
            if (_currentState != null)
            {
                _currentState.Exit(enemy);
            }

            // 새 상태로 교체
            _currentState = newState;

            // 새 상태가 있으면 Enter(시작) 호출
            // Enter에서 초기화 작업을 함 (예: 속도 설정, 이동 시작)
            if (_currentState != null)
            {
                _currentState.Enter(enemy);
                
                // 상태 이름 저장
                // is 키워드: 해당 타입인지 확인
                // 예: _currentState is IdleState → IdleState면 true
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

        // Update: 매 프레임 호출되는 함수
        // EnemyController의 Update에서 호출됨
        public void Update(EnemyController enemy)
        {
            // 현재 상태가 있으면 Execute(실행) 호출
            // Execute에서 상태의 메인 로직을 처리함 (플레이어 감지, 이동 등)
            if (_currentState != null)
            {
                _currentState.Execute(enemy);
            }
        }
    }
}