// [역할] 전투 FSM 상태 인터페이스
// 모든 전투 관련 상태 클래스는 이 인터페이스를 구현해야 함
public interface ICombatState
{
    void Enter(EnemyController enemy);   // 상태 진입 시 1회 호출
    void Execute(EnemyController enemy); // 매 프레임 호출
    void Exit(EnemyController enemy);    // 상태 종료 시 1회 호출
    
    // 전투 상태에서 이동 잠금이 필요한지 여부
    bool RequiresMovementLock { get; }
}
