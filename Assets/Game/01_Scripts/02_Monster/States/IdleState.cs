using UnityEngine;

// 대기 상태 - 제자리에서 대기 후 순찰 또는 추적으로 전환
public class IdleState : IEnemyState
{
    private float _elapsedTime = 0f;    // 대기 경과 시간
    private float _waitDuration = 1f;   // 대기 총 시간 (초)

    // 상태 진입: 이동 정지, 타이머 초기화
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.Stop();
        _elapsedTime = 0f;
        Debug.Log(enemy.gameObject.name + ": IdleState 진입");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        // 플레이어 감지 시 추적 상태로 전환
        if (enemy.Senses != null && enemy.Senses.TryDetectPlayer())
        {
            enemy.ChangeToChase();
            return;
        }

        // 대기 시간 완료 시 순찰 상태로 전환
        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= _waitDuration && enemy.Movement?.CanPatrol() == true)
        {
            enemy.ChangeToPatrol();
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log(enemy.gameObject.name + ": IdleState 종료");
    }
}
