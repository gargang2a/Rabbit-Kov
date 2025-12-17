using UnityEngine;

/// <summary>
/// [Role] 공격 대기 상태 - 쿨다운 체크 및 패턴 선택
/// EnemyCombat.CanAttack() 사용
/// </summary>
public class CombatReadyState : ICombatState
{
    // 대기 상태에서는 이동 잠금 불필요 (추격하면서 대기 가능)
    public bool RequiresMovementLock => false;

    public void Enter(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: CombatReadyState 진입");
    }

    public void Execute(EnemyController enemy)
    {
        // 타겟 소실 시 Inactive로 전환
        if (enemy.CurrentTarget == null)
        {
            enemy.ChangeCombatState(enemy.CombatInactiveState);
            return;
        }
        
        // 사거리 이탈 시 Inactive로 전환
        if (!enemy.Combat.IsTargetInRange())
        {
            enemy.ChangeCombatState(enemy.CombatInactiveState);
            return;
        }
        
        // 쿨다운 체크 + 공격 시작
        if (enemy.Combat.IsCooldownReady())
        {
            enemy.ChangeCombatState(enemy.CombatWindupState);
        }
    }

    public void Exit(EnemyController enemy)
    {
        // 정리 작업 없음
    }
}
