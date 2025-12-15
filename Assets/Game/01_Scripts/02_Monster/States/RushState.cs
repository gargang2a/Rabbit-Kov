using UnityEngine;

// 돌진 상태 - 일반 몬스터가 플레이어에게 직선으로 달려감
// Zone에 플레이어가 있으면 무조건 타겟을 향해 이동
public class RushState : IEnemyState
{
    // 상태 진입: 추격 속도 설정
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.SetChaseSpeed();
        Debug.Log(enemy.gameObject.name + ": RushState 진입");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // Zone에 플레이어 없거나 타겟 없으면 제자리 대기
        if (!enemy.IsPlayerInZone || !enemy.HasTarget())
        {
            enemy.Movement?.Stop();
            return; // Rush 상태 유지, 플레이어 재진입 대기
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
        enemy.Movement?.MoveTo(enemy.CurrentTarget.position);
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log(enemy.gameObject.name + ": RushState 종료");
    }
}
