using UnityEngine;

// 공격 상태 - 타겟을 바라보며 공격
public class AttackState : IEnemyState
{
    // 상태 진입: 이동 정지
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.Stop();
        Debug.Log(enemy.gameObject.name + ": AttackState 진입");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // 플레이어 Zone 이탈 시 대기 상태로 전환
        if (!enemy.IsPlayerInZone)
        {
            enemy.ClearTarget();
            enemy.ChangeToIdle();
            return;
        }
        
        // 타겟 소실 시 대기 상태로 전환
        if (enemy.CurrentTarget == null)
        {
            enemy.ChangeToIdle();
            return;
        }

        // 타겟 방향으로 회전
        enemy.Movement?.FaceTarget(enemy.CurrentTarget);

        float distance = Vector3.Distance(enemy.transform.position, enemy.CurrentTarget.position);

        // 사거리 이탈 시 추적 상태로 전환
        if (enemy.Combat != null && distance > enemy.Combat.AttackRange)
        {
            enemy.ChangeToChase();
            return;
        }

        // 공격 시도
        enemy.Combat?.TryAttack();
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log(enemy.gameObject.name + ": AttackState 종료");
    }
}
