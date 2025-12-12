using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 추격 상태 - 플레이어 향해 달려감, 공격범위 도달시 공격, 시야 놓치면 조사
    public class ChaseState : IEnemyState
    {
        private Vector3 _lastKnownPosition; // 마지막으로 플레이어 본 위치

        public void Enter(EnemyController enemy)
        {
            if (enemy.Movement != null)
            {
                enemy.Movement.SetRunSpeed();
            }

            if (enemy.CurrentTarget != null)
            {
                _lastKnownPosition = enemy.CurrentTarget.position;
            }

            Debug.Log(enemy.gameObject.name + ": ChaseState 진입");
        }

        public void Execute(EnemyController enemy)
        {
            // 타겟 없으면 마지막 위치로 조사
            if (enemy.CurrentTarget == null)
            {
                enemy.ChangeState(new InvestigateState(_lastKnownPosition));
                return;
            }

            _lastKnownPosition = enemy.CurrentTarget.position; // 위치 업데이트

            // 거리 계산
            Vector3 myPosition = enemy.transform.position;
            Vector3 targetPosition = enemy.CurrentTarget.position;
            float distance = Vector3.Distance(myPosition, targetPosition);

            // 공격 범위 안이면 공격
            if (enemy.Combat != null && distance <= enemy.Combat.AttackRange)
            {
                enemy.ChangeState(new AttackState());
                return;
            }

            // 타겟 향해 이동
            if (enemy.Movement != null)
            {
                enemy.Movement.MoveTo(targetPosition);
            }

            // 시야 놓치면 조사
            if (enemy.Senses != null && enemy.Senses.ScanForTarget() == false)
            {
                enemy.ChangeState(new InvestigateState(_lastKnownPosition));
                return;
            }
        }

        public void Exit(EnemyController enemy)
        {
            Debug.Log(enemy.gameObject.name + ": ChaseState 종료");
        }
    }
}
