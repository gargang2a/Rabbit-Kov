using UnityEngine;

// 돌진 상태 - 일반 몬스터가 플레이어에게 직선으로 달려감
public class RushState : IEnemyState
{
    // 상태 진입: 추격 속도 설정
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.SetChaseSpeed();
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // 플레이어가 Zone 밖이면 정지 및 타겟 해제
        if (!enemy.IsPlayerInZone)
        {
            enemy.Movement?.Stop();
            enemy.ClearTarget();
            return; // Rush 상태 유지, 플레이어 재진입 대기
        }
        
        // 타겟이 없으면 대기 (Zone 내 재감지 대기)
        if (!enemy.HasTarget())
        {
            enemy.Movement?.Stop();
            return;
        }

        // 거리 계산 (sqrMagnitude로 최적화)
        Vector3 diff = enemy.transform.position - enemy.CurrentTarget.position;
        float sqrDistance = diff.sqrMagnitude;
        float attackRangeSqr = enemy.Combat != null ? 
            enemy.Combat.AttackRange * enemy.Combat.AttackRange : 0f;

        // 공격 사거리 진입 시 Attack 전환
        if (enemy.Combat != null && sqrDistance <= attackRangeSqr)
        {
            enemy.ChangeToAttack();
            return;
        }

        // 타겟을 향해 돌진
        if (enemy.Movement != null)
        {
            enemy.Movement.MoveTo(enemy.CurrentTarget.position);
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: RushState 종료");
    }
}


