namespace RabbitKov.Enemy
{
    public class EnemyStateMachine
    {
        private IEnemyState _currentState;

        public string CurrentStateName
        {
            get
            {
                if (_currentState != null)
                {
                    return _currentState.GetType().Name;
                }
                else
                {
                    return "None";
                }
            }
        }

        public void ChangeState(IEnemyState newState, EnemyController enemy)
        {
            if (_currentState != null)
            {
                _currentState.Exit(enemy);
            }

            _currentState = newState;

            if (_currentState != null)
            {
                _currentState.Enter(enemy);
            }
        }

        public void Update(EnemyController enemy)
        {
            if (_currentState != null)
            {
                _currentState.Execute(enemy);
            }
        }
    }
}