using UnityEngine;

// [역할] 정지 상태 - 정지 공격 시 이동 잠금
public class StoppedState : IMovementState
{
    private bool _hasFacedTarget = false; // 회전 완료 여부
    
    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.Stop(); // 이동 정지
        _hasFacedTarget = false; // 리셋
        Debug.Log($"{enemy.gameObject.name}: StoppedState 진입 (이동 잠금)");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // [수정] 회전 완료 후에는 더 이상 FaceTarget() 호출하지 않음 (떨림 방지)
        if (!_hasFacedTarget && enemy.CurrentTarget != null)
        {
            bool rotationComplete = enemy.Movement?.FaceTarget(enemy.CurrentTarget) ?? true;
            if (rotationComplete)
            {
                _hasFacedTarget = true;
            }
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: StoppedState 종료");
    }
}
