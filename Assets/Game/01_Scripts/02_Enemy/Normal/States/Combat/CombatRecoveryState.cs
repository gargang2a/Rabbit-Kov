using UnityEngine;

// [역할] 공격 후딜 상태 - 공격 후 회복 시간
public class CombatRecoveryState : ICombatState
{
    public bool RequiresMovementLock => false; // 후딜 중 이동 가능
    
    private float _enterTime; // 진입 시간

    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        _enterTime = Time.time;
        
        float recoveryDuration = 0.5f;
        if (enemy.Combat != null)
        {
            recoveryDuration = enemy.Combat.RecoveryDuration;
        }
        
        Debug.Log($"{enemy.gameObject.name}: CombatRecoveryState 진입 (후딜 {recoveryDuration}초)");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        float recoveryDuration = 0.5f;
        if (enemy.Combat != null)
        {
            recoveryDuration = enemy.Combat.RecoveryDuration;
        }
        
        // 후딜 완료 체크
        if (Time.time >= _enterTime + recoveryDuration)
        {
            enemy.Combat?.EndAttack(); // 공격 종료 알림
            
            // 타겟 + 사거리 → Ready, 아니면 Inactive
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

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        // 정리 없음
    }
}
