// 적 AI 상태 인터페이스 (FSM 패턴)
// 모든 상태 클래스는 이 인터페이스를 구현해야 함
public interface IEnemyState
{
    void Enter(EnemyController enemy);   // 상태 진입 시 1회 호출
    void Execute(EnemyController enemy); // 매 프레임 호출
    void Exit(EnemyController enemy);    // 상태 종료 시 1회 호출
}
