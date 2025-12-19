using UnityEngine;

// [역할] 전투 비활성 상태 - 타겟 없거나 사거리 밖
public class CombatInactiveState : ICombatState
{
    public bool RequiresMovementLock => false; // 이동 잠금 불필요

    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        // 전투 비활성화
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        if (enemy.CurrentTarget == null) return; // 타겟 없으면 종료
        
        float distance = Vector3.Distance(
            enemy.transform.position, 
            enemy.CurrentTarget.position
        );
        
        // 사거리 진입 시 Ready로 전환
        if (distance <= enemy.Combat.AttackRange)
        {
            enemy.ChangeCombatState(enemy.CombatReadyState);
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        // 정리 없음
    }
}
