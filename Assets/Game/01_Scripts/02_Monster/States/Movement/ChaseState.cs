using UnityEngine;

// [역할] 추적 상태 - 타겟을 향해 이동 (병렬 FSM 버전)
// 전투는 CombatFSM이 병렬로 처리하므로 이동에만 집중
public class ChaseState : IMovementState
{
    private Vector3 _lastTargetPos;
    private float _targetLostTimer = 0f;
    private float _targetLostThreshold = 3f; // 타겟 소실 후 복귀까지 대기 시간

    /// <summary>
    /// 상태 진입: 추적 속도 설정
    /// </summary>
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.SetChaseSpeed();
        _targetLostTimer = 0f;

        // 현재 타겟 위치 저장
        if (enemy.CurrentTarget != null)
            _lastTargetPos = enemy.CurrentTarget.position;
        else
            _lastTargetPos = enemy.transform.position;

        Debug.Log($"{enemy.gameObject.name}: ChaseState 진입");
    }

    /// <summary>
    /// 매 프레임 실행: 타겟 추적만 담당 (공격은 CombatFSM이 처리)
    /// </summary>
    public void Execute(EnemyController enemy)
    {
        if (enemy.Movement == null) return;

        // === 타겟 소실 처리 ===
        if (enemy.CurrentTarget == null)
        {
            _targetLostTimer += Time.deltaTime;
            
            // 마지막 위치로 이동
            enemy.Movement.MoveTo(_lastTargetPos);
            
            // 일정 시간 후 복귀 상태로 전환
            if (_targetLostTimer >= _targetLostThreshold)
            {
                enemy.ChangeMovementState(enemy.ReturnMovementState);
            }
            return;
        }

        // === 타겟 존재 시 추적 ===
        _targetLostTimer = 0f;
        _lastTargetPos = enemy.CurrentTarget.position;

        // Zone 이탈 체크 (Normal 전용)
        // Epic은 EnemySenses가 거리 초과 시 자동으로 ClearTarget() 호출
        if (!enemy.IsEpic && !enemy.IsPlayerInZone)
        {
            enemy.ClearTarget();
            enemy.Movement?.Stop();
            enemy.ChangeMovementState(enemy.WaitMovementState);
            return;
        }

        // 타겟 추적 이동
        enemy.Movement.MoveTo(enemy.CurrentTarget.position);
    }

    /// <summary>
    /// 상태 종료
    /// </summary>
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: ChaseState 종료");
    }
}
