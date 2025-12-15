using UnityEngine;

// 추적 상태 - 플레이어를 향해 이동
public class ChaseState : IEnemyState
{
    private Vector3 _lastTargetPos;  // 마지막 타겟 위치 (조사용)

    // 상태 진입: 추적 속도 설정, 타겟 위치 저장
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.SetChaseSpeed();

        // 현재 타겟 위치 저장 (놓쳤을 때 조사 위치로 사용)
        if (enemy.CurrentTarget != null)
            _lastTargetPos = enemy.CurrentTarget.position;
        else
            _lastTargetPos = enemy.transform.position;

        Debug.Log(enemy.gameObject.name + ": ChaseState 진입");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        if (enemy.Movement == null)
        {
            enemy.ChangeState(new InvestigateState(_lastTargetPos));
            return;
        }

        // 타겟 소실 시 마지막 위치 조사
        if (enemy.CurrentTarget == null)
        {
            enemy.ChangeState(new InvestigateState(_lastTargetPos));
            return;
        }

        // 매 프레임 타겟 위치 갱신
        _lastTargetPos = enemy.CurrentTarget.position;

        float distance = Vector3.Distance(enemy.transform.position, enemy.CurrentTarget.position);

        // 공격 사거리 진입 시 공격 상태로 전환
        if (enemy.Combat != null && distance <= enemy.Combat.AttackRange)
        {
            enemy.ChangeToAttack();
            return;
        }

        // 타겟 추적
        enemy.Movement.MoveTo(enemy.CurrentTarget.position);

        // 플레이어 Zone 이탈 시 추적 중단
        if (!enemy.IsPlayerInZone)
        {
            enemy.ClearTarget();
            enemy.ChangeToIdle();
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log(enemy.gameObject.name + ": ChaseState 종료");
    }
}
