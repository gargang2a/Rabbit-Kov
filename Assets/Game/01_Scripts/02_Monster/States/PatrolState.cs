using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 정찰 상태 - 랜덤/웨이포인트 순회, 도착하면 잠시 대기 후 다음으로
    public class PatrolState : IEnemyState
    {
        private float _waitTimer = 0f;    // 도착 후 대기한 시간
        private float _waitDuration = 1f; // 도착 후 대기할 시간(초)
        private bool _isWaiting = false;  // 현재 대기 중?

        public void Enter(EnemyController enemy)
        {
            if (enemy.Movement == null) return;

            if (enemy.Movement.UseRandomPatrol)
            {
                enemy.Movement.MoveToRandomPoint();
            }
            else
            {
                enemy.Movement.MoveToNextPatrolPoint();
            }

            Debug.Log(enemy.gameObject.name + ": PatrolState 진입");
        }

        public void Execute(EnemyController enemy)
        {
            // 플레이어 발견하면 추격
            if (enemy.Senses != null && enemy.Senses.ScanForTarget())
            {
                enemy.ChangeState(new ChaseState());
                return;
            }

            if (enemy.Movement == null) return;

            // 대기 중
            if (_isWaiting)
            {
                _waitTimer = _waitTimer + Time.deltaTime;
                
                if (_waitTimer >= _waitDuration)
                {
                    _isWaiting = false;
                    _waitTimer = 0f;

                    // 다음 정찰 지점으로
                    if (enemy.Movement.UseRandomPatrol)
                    {
                        enemy.Movement.MoveToRandomPoint();
                    }
                    else
                    {
                        enemy.Movement.AdvancePatrolIndex();
                        enemy.Movement.MoveToNextPatrolPoint();
                    }
                }
                return;
            }

            // 이동 중 - 도착하면 대기 시작
            if (enemy.Movement.HasReachedDestination)
            {
                _isWaiting = true;
            }
        }

        public void Exit(EnemyController enemy)
        {
            Debug.Log(enemy.gameObject.name + ": PatrolState 종료");
        }
    }
}
