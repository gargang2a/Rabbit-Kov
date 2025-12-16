using UnityEngine;

// [역할] 공격 실행 상태 - 실제 데미지 판정
public class CombatAttackingState : ICombatState
{
    // 공격 중 이동 잠금 유지
    public bool RequiresMovementLock => true;
    
    private float _attackDuration = 0.4f; // 공격 실행 시간 (초)
    private float _enterTime;
    private bool _hasDealtDamage = false;

    public void Enter(EnemyController enemy)
    {
        _enterTime = Time.time;
        _hasDealtDamage = false;
        Debug.Log($"{enemy.gameObject.name}: CombatAttackingState 진입 (공격!)");
        
        // TODO: 공격 애니메이션 재생
    }

    public void Execute(EnemyController enemy)
    {
        // 공격 중반에 데미지 판정 (애니메이션 타이밍과 맞춤)
        float elapsed = Time.time - _enterTime;
        
        if (!_hasDealtDamage && elapsed >= _attackDuration * 0.5f)
        {
            DealDamage(enemy);
            _hasDealtDamage = true;
        }
        
        // 공격 완료 후 Recovery로 전환
        if (elapsed >= _attackDuration)
        {
            enemy.ChangeCombatState(enemy.CombatRecoveryState);
        }
    }
    
    /// <summary>
    /// 실제 데미지 처리
    /// </summary>
    private void DealDamage(EnemyController enemy)
    {
        if (enemy.CurrentTarget == null) return;
        
        // 사거리 재확인
        float distance = Vector3.Distance(
            enemy.transform.position, 
            enemy.CurrentTarget.position
        );
        
        if (distance > enemy.Combat.AttackRange * 1.2f) return; // 약간의 여유
        
        // IDamageable 인터페이스로 데미지 전달
        if (enemy.CurrentTarget.TryGetComponent(out IDamageable target))
        {
            Vector3 attackDir = (enemy.CurrentTarget.position - enemy.transform.position).normalized;
            target.TakeDamage(enemy.Combat.AttackDamage, enemy.CurrentTarget.position, attackDir);
            Debug.Log($"{enemy.gameObject.name} → {enemy.CurrentTarget.name} ({enemy.Combat.AttackDamage} damage)");
        }
    }

    public void Exit(EnemyController enemy)
    {
        // 정리 작업 없음
    }
}
