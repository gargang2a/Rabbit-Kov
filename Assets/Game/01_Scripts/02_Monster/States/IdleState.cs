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
        // 타겟 감지 시 상태 전환
        if (enemy.HasTarget())
        {
            // 에픽: Chase로 전환, 일반: Rush로 전환
            if (enemy.IsEpic)
                enemy.ChangeToChase();
            else
                enemy.ChangeToRush();
            return;
        }

        // 대기 시간 완료 시 상태 전환 (Epic만 순찰)
        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= _waitDuration)
        {
            // 에픽: Patrol로 전환, 일반: Rush 유지 (플레이어 재진입 대기)
            if (enemy.IsEpic && enemy.Movement?.CanPatrol() == true)
                enemy.ChangeToPatrol();
            else if (!enemy.IsEpic)
                enemy.ChangeToRush();
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log(enemy.gameObject.name + ": IdleState 종료");
    }
}
