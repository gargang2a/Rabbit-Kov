using UnityEngine;

// [역할] 스턴 이동 상태 - 완전 정지 (Epic/Boss)
public class StunnedMovementState : IMovementState
{
    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.Stop(); // 이동 정지
        Debug.Log($"[Stun] {enemy.name}: 스턴 상태 진입 - 이동 정지");
        
        // TODO: 스턴 VFX 재생
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // 스턴 중 아무것도 안 함 (완전 경직)
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"[Stun] {enemy.name}: 스턴 상태 종료");
        
        // TODO: 스턴 VFX 정지
    }
}
