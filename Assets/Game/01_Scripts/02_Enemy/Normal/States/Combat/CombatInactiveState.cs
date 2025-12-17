using UnityEngine;

// [역할] 전투 비활성 상태 - 타겟이 없거나 사거리 밖
public class CombatInactiveState : ICombatState
{
    // 비활성 상태에서는 이동 잠금 불필요
    public bool RequiresMovementLock => false;

    public void Enter(EnemyController enemy)
    {
        // 전투 비활성화
    }

    public void Execute(EnemyController enemy)
    {
        // 타겟 존재 + 사거리 진입 체크
        if (enemy.CurrentTarget == null) return;
        
        float distance = Vector3.Distance(
            enemy.transform.position, 
            enemy.CurrentTarget.position
        );
        
        // 사거리 진입 시 Ready 상태로 전환
        if (distance <= enemy.Combat.AttackRange)
        {
            enemy.ChangeCombatState(enemy.CombatReadyState);
        }
    }

    public void Exit(EnemyController enemy)
    {
        // 정리 작업 없음
    }
}
