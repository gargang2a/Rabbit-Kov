using UnityEngine;

/// <summary>
/// [Role] 공격 실행 상태 - 실제 데미지 판정
/// EnemyCombat.ExecuteDamage() 호출
/// </summary>
public class CombatAttackingState : ICombatState
{
    // 공격 중 이동 잠금 유지
    public bool RequiresMovementLock => true;
    
    private float _enterTime;
    private bool _hasDealtDamage = false;

    public void Enter(EnemyController enemy)
    {
        _enterTime = Time.time;
        _hasDealtDamage = false;
        Debug.Log($"{enemy.gameObject.name}: CombatAttackingState 진입 (공격!)");
    }

    public void Execute(EnemyController enemy)
    {
        float attackDuration = enemy.Combat?.AttackDuration ?? 0.2f;
        float elapsed = Time.time - _enterTime;
        
        // 공격 중반에 데미지 판정 (애니메이션 타이밍과 맞춤)
        if (!_hasDealtDamage && elapsed >= attackDuration * 0.5f)
        {
            enemy.Combat?.ExecuteDamage();
            _hasDealtDamage = true;
        }
        
        // 공격 완료 후 Recovery로 전환
        if (elapsed >= attackDuration)
        {
            enemy.ChangeCombatState(enemy.CombatRecoveryState);
        }
    }

    public void Exit(EnemyController enemy)
    {
        // 정리 작업 없음
    }
}
