namespace RabbitKov.Enemy
{
    public interface IEnemyState
    {
        void Enter(EnemyController enemy);

        void Execute(EnemyController enemy);

        void Exit(EnemyController enemy);
    }
}