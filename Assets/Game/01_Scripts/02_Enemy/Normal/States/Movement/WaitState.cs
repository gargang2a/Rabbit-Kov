using UnityEngine;

// [역할] 대기 상태 - Normal 몬스터 전용, Zone 진입 대기
public class WaitState : IMovementState
{
    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.Stop(); // 정지
        Debug.Log($"{enemy.gameObject.name}: WaitState 진입 (대기 중)");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // 타겟 + Zone 진입 시 추적
        if (enemy.HasTarget() && enemy.IsPlayerInZone)
        {
            enemy.ChangeMovementState(enemy.ChaseMovementState);
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: WaitState 종료");
    }
}
