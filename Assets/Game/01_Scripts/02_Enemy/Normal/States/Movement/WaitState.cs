using UnityEngine;

// [역할] 대기 상태 - Normal 몬스터 전용
// 플레이어가 Zone에 진입할 때까지 정지 상태로 대기
public class WaitState : IMovementState
{
    /// <summary>
    /// 상태 진입: 즉시 정지
    /// </summary>
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.Stop();
        Debug.Log($"{enemy.gameObject.name}: WaitState 진입 (대기 중)");
    }

    /// <summary>
    /// 매 프레임 실행: 타겟이 설정되면 추적 시작
    /// </summary>
    public void Execute(EnemyController enemy)
    {
        // 플레이어가 Zone에 진입하면 (EnemySpawner가 SetTarget 호출)
        // → ChaseState로 전환
        if (enemy.HasTarget() && enemy.IsPlayerInZone)
        {
            enemy.ChangeMovementState(enemy.ChaseMovementState);
        }
    }

    /// <summary>
    /// 상태 종료
    /// </summary>
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: WaitState 종료");
    }
}
