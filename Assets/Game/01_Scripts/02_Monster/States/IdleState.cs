using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 대기 상태 - 가만히 서서 주변 살핌, 일정 시간 후 정찰
    public class IdleState : IEnemyState
    {
        private float _idleTimer = 0f;    // 대기한 시간
        private float _idleDuration = 2f; // 대기할 총 시간(초)

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
            // 플레이어 감지하면 추격
            if (enemy.Senses != null && enemy.Senses.ScanForTarget())
            {
                enemy.ChangeState(new ChaseState());
                return;
            }

            _idleTimer = _idleTimer + Time.deltaTime;

            // 대기 시간 지나면 정찰
            if (_idleTimer >= _idleDuration)
            {
                if (enemy.Movement != null && enemy.Movement.CanPatrol())
                {
                    enemy.ChangeState(new PatrolState());
                    return;
                }
            }
        }

        public void Exit(EnemyController enemy)
        {
            Debug.Log(enemy.gameObject.name + ": IdleState 종료");
        }
    }
}
