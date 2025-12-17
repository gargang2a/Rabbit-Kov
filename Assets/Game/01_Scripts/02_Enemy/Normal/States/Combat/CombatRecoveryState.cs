using UnityEngine;

/// <summary>
/// [Role] 공격 후딜 상태 - 공격 후 회복 시간
/// EnemyCombat.RecoveryDuration 사용
/// </summary>
public class CombatRecoveryState : ICombatState
{
    // 후딜 중에는 이동 잠금 해제 가능
    public bool RequiresMovementLock => false;
    
    private float _enterTime;

    public void Enter(EnemyController enemy)
    {
        _enterTime = Time.time;
        Debug.Log($"{enemy.gameObject.name}: CombatRecoveryState 진입 (후딜 {enemy.Combat?.RecoveryDuration ?? 0.5f}초)");
    }

    public void Execute(EnemyController enemy)
    {
        float recoveryDuration = enemy.Combat?.RecoveryDuration ?? 0.5f;
        
        // 후딜 완료 체크
        if (Time.time >= _enterTime + recoveryDuration)
        {
            enemy.Combat?.EndAttack(); // 공격 종료 알림
            
            // 타겟 존재 + 사거리 내 → Ready로, 아니면 Inactive로
            if (enemy.CurrentTarget != null && enemy.Combat.IsTargetInRange())
            {
                enemy.ChangeCombatState(enemy.CombatReadyState);
            }
            else
            {
                enemy.ChangeCombatState(enemy.CombatInactiveState);
            }
        }
    }

    public void Exit(EnemyController enemy)
    {
        // 정리 작업 없음
    }
}
