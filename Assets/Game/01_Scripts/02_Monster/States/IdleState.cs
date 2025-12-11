using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    public class IdleState : IEnemyState
    {
        private float _idleTimer = 0f;
        private float _idleDuration = 2f;

        public void Enter(EnemyController enemy)
        {
            if (enemy.Movement != null)
            {
                enemy.Movement.Stop();
            }

            _idleTimer = 0f;

            Debug.Log(enemy.gameObject.name + ": IdleState 진입");
        }

        public void Execute(EnemyController enemy)
        {

            if (enemy.DetectPlayer())
            {
                enemy.ChangeState(new InvestigateState());
                return;
            }

            _idleTimer += Time.deltaTime;

            if (_idleTimer >= _idleDuration)
            {
                if (enemy.Movement != null)
                {
                    if (enemy.Movement.CanPatrol())
                    {
                        enemy.ChangeState(new PatrolState());
                        return;
                    }
                }
            }
        }

        public void Exit(EnemyController enemy)
        {
            Debug.Log(enemy.gameObject.name + ": IdleState 종료");
        }
    }
}
