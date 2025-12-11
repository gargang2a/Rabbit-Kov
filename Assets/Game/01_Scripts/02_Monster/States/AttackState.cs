using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 공격 상태 - 타겟 바라보며 공격, 범위 밖 나가면 추격
    public class AttackState : IEnemyState
    {
        public void Enter(EnemyController enemy)
        {
            if (enemy.Movement != null)
            {
                enemy.Movement.Stop(); // 공격 중엔 멈춤
            }
            Debug.Log(enemy.gameObject.name + ": AttackState 진입");
        }

        public void Execute(EnemyController enemy)
        {
            // 타겟 없으면 대기
            if (enemy.CurrentTarget == null)
            {
                enemy.ChangeState(new IdleState());
                return;
            }

            // 타겟 바라봄
            if (enemy.Movement != null)
            {
                enemy.Movement.LookAt(enemy.CurrentTarget);
            }

            // 거리 계산
            Vector3 myPosition = enemy.transform.position;
            Vector3 targetPosition = enemy.CurrentTarget.position;
            float distance = Vector3.Distance(myPosition, targetPosition);

            // 범위 밖이면 추격
            if (enemy.Combat != null && distance > enemy.Combat.AttackRange)
            {
                enemy.ChangeState(new ChaseState());
                return;
            }

            // 공격 시도 (쿨타임은 TryAttack 내부에서 처리)
            if (enemy.Combat != null)
            {
                enemy.Combat.TryAttack();
            }
        }

        public void Exit(EnemyController enemy)
        {
            Debug.Log(enemy.gameObject.name + ": AttackState 종료");
        }
    }
}
