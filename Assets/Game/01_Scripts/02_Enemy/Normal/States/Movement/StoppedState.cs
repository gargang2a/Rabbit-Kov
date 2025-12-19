using UnityEngine;

// [역할] 정지 상태 - 정지 공격 시 이동 잠금
public class StoppedState : IMovementState
{
    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.Stop(); // 이동 정지
        Debug.Log($"{enemy.gameObject.name}: StoppedState 진입 (이동 잠금)");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // 정지 상태에서 타겟 방향만 응시
        if (enemy.CurrentTarget != null)
        {
            enemy.Movement?.FaceTarget(enemy.CurrentTarget);
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: StoppedState 종료");
    }
}
