using UnityEngine;

// [역할] 정지 상태 - 정지 공격 시 이동 잠금 상태
// 전투 FSM에서 정지가 필요한 공격 실행 시 강제 진입
public class StoppedState : IMovementState
{
    /// <summary>
    /// 상태 진입: 이동 정지
    /// </summary>
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.Stop();
        Debug.Log($"{enemy.gameObject.name}: StoppedState 진입 (이동 잠금)");
    }

    /// <summary>
    /// 매 프레임 실행: 이동 잠금 상태이므로 아무것도 하지 않음
    /// 전투 FSM이 공격을 처리하고, 완료 시 UnlockMovement() 호출
    /// </summary>
    public void Execute(EnemyController enemy)
    {
        // 정지 상태에서는 타겟 방향만 바라봄 (공격 정확도 유지)
        if (enemy.CurrentTarget != null)
        {
            enemy.Movement?.FaceTarget(enemy.CurrentTarget);
        }
    }

    /// <summary>
    /// 상태 종료
    /// </summary>
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: StoppedState 종료");
    }
}
