using UnityEngine;

// [역할] 공격 대기 상태 - 쿨다운 체크 및 공격 준비
public class CombatReadyState : ICombatState
{
    public bool RequiresMovementLock => false; // 이동 잠금 불필요

    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: CombatReadyState 진입");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // 타겟 소실 시 Inactive로
        if (enemy.CurrentTarget == null)
        {
            enemy.ChangeCombatState(enemy.CombatInactiveState);
            return;
        }
        
        // 사거리 이탈 시 Inactive로
        if (!enemy.Combat.IsTargetInRange())
        {
            enemy.ChangeCombatState(enemy.CombatInactiveState);
            return;
        }
        
        // 쿨다운 완료 시 공격 시작
        if (enemy.Combat.IsCooldownReady())
        {
            enemy.ChangeCombatState(enemy.CombatWindupState);
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        // 정리 없음
    }
}
