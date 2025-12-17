using UnityEngine;

/// <summary>
/// [Role] 스턴 이동 상태 - 완전 정지
/// 스턴 중에는 이동 불가, 스턴 종료 시 이전 상태로 복귀
/// </summary>
public class StunnedMovementState : IMovementState
{
    /// <summary>
    /// 상태 진입: 이동 정지
    /// </summary>
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.Stop();
        Debug.Log($"[Stun] {enemy.name}: 스턴 상태 진입 - 이동 정지");
        
        // TODO: 스턴 VFX 재생 (파티클, 머리 위 별 등)
    }

    /// <summary>
    /// 매 프레임 실행: 아무것도 하지 않음 (완전 정지)
    /// 스턴 종료 체크는 EnemyController.LateUpdate()에서 처리
    /// </summary>
    public void Execute(EnemyController enemy)
    {
        // 스턴 중에는 아무 동작 없음
        // 타겟 방향 응시도 하지 않음 (완전 경직)
    }

    /// <summary>
    /// 상태 종료: 정리
    /// </summary>
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"[Stun] {enemy.name}: 스턴 상태 종료");
        
        // TODO: 스턴 VFX 정지
    }
}
