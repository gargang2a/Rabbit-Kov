using UnityEngine;

// [역할] 공격 실행 상태 - 실제 데미지 판정
public class CombatAttackingState : ICombatState
{
    public bool RequiresMovementLock => true; // 이동 잠금 필요
    
    private float _enterTime;             // 진입 시간
    private bool _hasDealtDamage = false; // 데미지 판정 완료 여부

    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        _enterTime = Time.time;
        _hasDealtDamage = false;
        Debug.Log($"{enemy.gameObject.name}: CombatAttackingState 진입 (공격!)");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        float attackDuration = 0.2f;
        if (enemy.Combat != null)
        {
            attackDuration = enemy.Combat.AttackDuration;
        }
        
        float elapsed = Time.time - _enterTime;
        
        // 공격 중반에 데미지 판정
        if (!_hasDealtDamage && elapsed >= attackDuration * 0.5f)
        {
            enemy.Combat?.ExecuteDamage();
            _hasDealtDamage = true;
        }
        
        // 공격 완료 후 Recovery로
        if (elapsed >= attackDuration)
        {
            enemy.ChangeCombatState(enemy.CombatRecoveryState);
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        // 정리 없음
    }
}
