using UnityEngine;

// [역할] 공격 선딜 상태 - 공격 준비 동작
public class CombatWindupState : ICombatState
{
    public bool RequiresMovementLock => true; // 이동 잠금 필요
    
    private float _enterTime; // 진입 시간

    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        _enterTime = Time.time;
        enemy.Combat?.StartAttack(); // 공격 시작 알림
        
        float windupDuration = 0.3f;
        if (enemy.Combat != null)
        {
            windupDuration = enemy.Combat.WindupDuration;
        }
        
        Debug.Log($"{enemy.gameObject.name}: CombatWindupState 진입 (선딜 {windupDuration}초)");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // 타겟 방향 유지
        if (enemy.CurrentTarget != null)
        {
            enemy.Movement?.FaceTarget(enemy.CurrentTarget);
        }
        
        // 선딜 완료 시 공격 실행
        float windupDuration = 0.3f;
        if (enemy.Combat != null)
        {
            windupDuration = enemy.Combat.WindupDuration;
        }
        
        if (Time.time >= _enterTime + windupDuration)
        {
            enemy.ChangeCombatState(enemy.CombatAttackingState);
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        // 정리 없음
    }
}
