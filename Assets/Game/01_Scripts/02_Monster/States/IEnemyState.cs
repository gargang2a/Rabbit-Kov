namespace RabbitKov.Enemy
{
    // FSM 상태 인터페이스 - 모든 상태가 이걸 구현해야 함
    public interface IEnemyState
    {
        void Enter(EnemyController enemy);   // 상태 진입 시 1번 호출
        void Execute(EnemyController enemy); // 매 프레임 호출
        void Exit(EnemyController enemy);    // 상태 종료 시 1번 호출
    }
}