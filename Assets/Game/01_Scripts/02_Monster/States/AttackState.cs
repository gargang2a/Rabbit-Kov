using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 공격 상태
    // 타겟을 바라보며 공격하는 상태
    // 타겟이 범위 밖으로 나가면 추격으로 전환
    
    public class AttackState : IEnemyState
    {
        public void Enter(EnemyController enemy)
        {
            // 공격할 때는 멈춰야 함 (이동하면서 공격하면 부자연스러움)
            if (enemy.Movement != null)
            {
                enemy.Movement.Stop();
            }
            Debug.Log(enemy.gameObject.name + ": AttackState 진입");
        }

        public void Execute(EnemyController enemy)
        {
            // 타겟이 없으면 대기 상태로 전환
            if (enemy.CurrentTarget == null)
            {
                enemy.ChangeState(new IdleState());
                return;
            }

            // 타겟을 바라봄
            // LookAt은 Quaternion으로 부드럽게 회전함
            if (enemy.Movement != null)
            {
                enemy.Movement.LookAt(enemy.CurrentTarget);
            }

            // 거리 계산
            Vector3 myPosition = enemy.transform.position;
            Vector3 targetPosition = enemy.CurrentTarget.position;
            float distance = Vector3.Distance(myPosition, targetPosition);

            // 공격 범위 밖으로 나갔는지 확인
            if (enemy.Combat != null && distance > enemy.Combat.AttackRange)
            {
                // 다시 추격
                enemy.ChangeState(new ChaseState());
                return;
            }

            // 공격 시도
            // TryAttack은 내부에서 쿨타임을 체크해서 쿨타임 중이면 공격 안 함
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
