using UnityEngine;

/// <summary>
/// [Role] 공격 선딜 상태 - 공격 준비 동작 (애니메이션 등)
/// EnemyCombat.WindupDuration 사용
/// </summary>
public class CombatWindupState : ICombatState
{
    // EnemyCombat에서 동적으로 결정
    public bool RequiresMovementLock => true;
    
    private float _enterTime;

    public void Enter(EnemyController enemy)
    {
        _enterTime = Time.time;
        enemy.Combat?.StartAttack(); // 공격 시작 알림
        Debug.Log($"{enemy.gameObject.name}: CombatWindupState 진입 (선딜 {enemy.Combat?.WindupDuration ?? 0.3f}초)");
    }

    public void Execute(EnemyController enemy)
    {
        // 타겟 방향 유지
        if (enemy.CurrentTarget != null)
        {
            enemy.Movement?.FaceTarget(enemy.CurrentTarget);
        }
        
        // 선딜 시간 경과 후 공격 실행
        float windupDuration = enemy.Combat?.WindupDuration ?? 0.3f;
        if (Time.time >= _enterTime + windupDuration)
        {
            enemy.ChangeCombatState(enemy.CombatAttackingState);
        }
    }

    public void Exit(EnemyController enemy)
    {
        // 정리 작업 없음
    }
}
