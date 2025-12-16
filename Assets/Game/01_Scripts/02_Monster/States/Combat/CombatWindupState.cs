using UnityEngine;

// [역할] 공격 선딜 상태 - 공격 준비 동작 (애니메이션 등)
public class CombatWindupState : ICombatState
{
    // 기본값: 이동 잠금 필요 (정지 공격)
    // TODO: 공격 패턴에 따라 동적으로 변경
    public bool RequiresMovementLock => true;
    
    private float _windupDuration = 0.3f; // 선딜 시간 (초)
    private float _enterTime;

    public void Enter(EnemyController enemy)
    {
        _enterTime = Time.time;
        Debug.Log($"{enemy.gameObject.name}: CombatWindupState 진입 (선딜)");
        
        // TODO: 선딜 애니메이션 재생
    }

    public void Execute(EnemyController enemy)
    {
        // 타겟 방향 유지
        if (enemy.CurrentTarget != null)
        {
            enemy.Movement?.FaceTarget(enemy.CurrentTarget);
        }
        
        // 선딜 시간 경과 후 공격 실행
        if (Time.time >= _enterTime + _windupDuration)
        {
            enemy.ChangeCombatState(enemy.CombatAttackingState);
        }
    }

    public void Exit(EnemyController enemy)
    {
        // 정리 작업 없음
    }
}
