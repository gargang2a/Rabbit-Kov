using UnityEngine;

// [역할] 공격 후딜 상태 - 공격 후 회복 시간
public class CombatRecoveryState : ICombatState
{
    // 후딜 중에는 이동 잠금 해제 가능
    public bool RequiresMovementLock => false;
    
    private float _recoveryDuration = 0.3f; // 후딜 시간 (초)
    private float _enterTime;

    public void Enter(EnemyController enemy)
    {
        _enterTime = Time.time;
        Debug.Log($"{enemy.gameObject.name}: CombatRecoveryState 진입 (후딜)");
        
        // TODO: 후딜 애니메이션 재생
    }

    public void Execute(EnemyController enemy)
    {
        // 후딜 완료 체크
        if (Time.time >= _enterTime + _recoveryDuration)
        {
            // 타겟 존재 시 Ready로, 없으면 Inactive로
            if (enemy.CurrentTarget != null)
            {
                float distance = Vector3.Distance(
                    enemy.transform.position, 
                    enemy.CurrentTarget.position
                );
                
                if (distance <= enemy.Combat.AttackRange)
                {
                    enemy.ChangeCombatState(enemy.CombatReadyState);
                    return;
                }
            }
            
            enemy.ChangeCombatState(enemy.CombatInactiveState);
        }
    }

    public void Exit(EnemyController enemy)
    {
        // 정리 작업 없음
    }
}
