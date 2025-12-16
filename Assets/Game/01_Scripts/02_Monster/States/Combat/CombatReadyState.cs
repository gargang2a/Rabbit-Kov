using UnityEngine;

// [역할] 공격 대기 상태 - 쿨다운 체크 및 패턴 선택
public class CombatReadyState : ICombatState
{
    // 대기 상태에서는 이동 잠금 불필요 (추격하면서 대기 가능)
    public bool RequiresMovementLock => false;
    
    private float _lastAttackTime = -999f;
    private float _attackCooldown = 1.5f; // 기본 공격 쿨다운

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
        float distance = Vector3.Distance(
            enemy.transform.position, 
            enemy.CurrentTarget.position
        );
        
        if (distance > enemy.Combat.AttackRange)
        {
            enemy.ChangeCombatState(enemy.CombatInactiveState);
            return;
        }
        
        // 쿨다운 체크
        if (Time.time < _lastAttackTime + _attackCooldown) return;
        
        // 공격 시작 (TODO: 패턴 선택 로직 추가)
        _lastAttackTime = Time.time;
        enemy.ChangeCombatState(enemy.CombatWindupState);
    }

    public void Exit(EnemyController enemy)
    {
        // 정리 작업 없음
    }
}
