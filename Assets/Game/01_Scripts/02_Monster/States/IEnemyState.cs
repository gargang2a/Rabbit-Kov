// ============================================================================
// IEnemyState - 적 AI 상태 인터페이스
// ============================================================================
// 
// [개념 설명: FSM (Finite State Machine, 유한 상태 기계)]
// FSM은 AI를 여러 "상태(State)"로 나누고, 조건에 따라 상태를 전환하는 디자인 패턴입니다.
// 
// 예시: 적이 "순찰 중" → 플레이어 발견 → "추적 중" → 사거리 도달 → "공격 중"
// 
// [왜 인터페이스를 사용하는가?]
// - 모든 상태가 동일한 메서드(Enter, Execute, Exit)를 구현하도록 강제합니다.
// - EnemyStateMachine이 구체적인 상태 클래스를 몰라도 상태를 교체할 수 있습니다.
// - 새로운 상태를 추가할 때 기존 코드를 수정하지 않아도 됩니다 (확장성).
// 
// [상태 전환 흐름]
// 1. 현재 상태의 Exit() 호출 → 정리 작업 (타이머 리셋 등)
// 2. 새 상태의 Enter() 호출 → 초기화 작업 (속도 설정, 이동 시작 등)
// 3. 매 프레임 현재 상태의 Execute() 호출 → 실제 로직 실행
// 
// [구현된 상태 목록]
// - IdleState: 대기 상태 (아무것도 안 함, 일정 시간 후 PatrolState로 전환)
// - PatrolState: 순찰 상태 (랜덤하게 돌아다님)
// - ChaseState: 추적 상태 (플레이어를 향해 달려감)
// - AttackState: 공격 상태 (플레이어가 사거리 내에 있으면 공격)
// - InvestigateState: 조사 상태 (플레이어를 놓친 후 마지막 위치 조사)
// ============================================================================
public interface IEnemyState
{
    // Enter: 상태 진입 시 한 번 호출됩니다.
    // - 용도: 초기화 작업 (이동 속도 설정, 타이머 리셋, 목적지 설정 등)
    // - 예시: ChaseState.Enter()에서 SetRunSpeed() 호출
    void Enter(EnemyController enemy);
    
    // Execute: 상태가 활성화된 동안 매 프레임 호출됩니다 (Update처럼 동작).
    // - 용도: 상태별 메인 로직 실행 및 상태 전환 조건 체크
    // - 예시: PatrolState.Execute()에서 목적지 도착 확인, 플레이어 감지 시 ChaseState로 전환
    void Execute(EnemyController enemy);
    
    // Exit: 상태 종료 시 한 번 호출됩니다 (다른 상태로 전환되기 직전).
    // - 용도: 정리 작업 (타이머 리셋, 플래그 초기화 등)
    // - 예시: InvestigateState.Exit()에서 _lookAroundTimer 리셋
    void Exit(EnemyController enemy);
}
