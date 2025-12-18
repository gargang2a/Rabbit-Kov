using UnityEngine;

// [역할] 추적 상태 - 타겟을 향해 이동 (병렬 FSM 버전)
// 전투는 CombatFSM이 병렬로 처리하므로 이동에만 집중
public class ChaseState : IMovementState
{
    private Vector3 _lastTargetPos;
    private float _targetLostTimer = 0f;
    private float _targetLostThreshold = 3f; // 타겟 소실 후 복귀까지 대기 시간
    private float _nextLogTime; // 디버그 로그 타이머
    private float _moveTimer = 0f;
    
    // [최적화] 거리 기반 동적 스로틀링 임계값
    private const float CLOSE_RANGE = 10f;     // 0~10m: 근접
    private const float MID_RANGE = 30f;       // 10~30m: 중거리
    private const float CLOSE_INTERVAL = 0.1f; // 근접: 0.1초마다 갱신
    private const float MID_INTERVAL = 0.3f;   // 중거리: 0.3초마다 갱신
    private const float FAR_INTERVAL = 0.6f;   // 원거리: 0.6초마다 갱신
    
    private Vector3 _lastMovedPos; // 마지막으로 이동 명령 내린 위치

    /// <summary>
    /// 상태 진입: 추적 속도 설정
    /// </summary>
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.SetChaseSpeed();
        _targetLostTimer = 0f;
        
        // [최적화] Time Slicing: 각 적마다 랜덤 오프셋으로 경로 계산 시점 분산
        // 모든 적이 동시에 CalculatePath를 호출하는 것을 방지
        _moveTimer = Random.Range(0f, FAR_INTERVAL);

        // 현재 타겟 위치 저장
        if (enemy.CurrentTarget != null)
            _lastMovedPos = enemy.CurrentTarget.position;
        else
            _lastMovedPos = enemy.transform.position;
            
        _lastTargetPos = _lastMovedPos;

        // Debug.Log($"{enemy.gameObject.name}: ChaseState 진입");
    }

    /// <summary>
    /// 매 프레임 실행: 타겟 추적만 담당 (공격은 CombatFSM이 처리)
    /// </summary>
    public void Execute(EnemyController enemy)
    {
        if (enemy.Movement == null) return;

        // [Normal Enemy 특수 처리] 
        // 타겟을 잃어버렸을 때(null), 마지막 위치로 이동하지 말고 즉시 플레이어를 다시 잡도록 강제함.
        if (enemy.CurrentTarget == null && !enemy.RestrictToZone)
        {
            Debug.LogWarning($"[Chase] {enemy.name}: Normal - 타겟 없음! 플레이어 재검색 시도...");
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                enemy.SetTarget(player.transform);
                Debug.Log($"[Chase] {enemy.name}: 타겟 재할당 완료 -> {player.name}");
            }
            else
            {
                Debug.LogError($"[Chase] {enemy.name}: 플레이어를 찾을 수 없음!");
            }
        }

        // === 타겟 소실 처리 ===
        if (enemy.CurrentTarget == null)
        {
            _targetLostTimer += Time.deltaTime;
            
            // 소실 후에도 마지막 위치로 이동 (주기적 호출)
            // 타겟 소실 시에도 마지막 위치로 이동 (FAR_INTERVAL 사용)
            _moveTimer += Time.deltaTime;
            if (_moveTimer >= FAR_INTERVAL)
            {
                _moveTimer = 0f;
                enemy.Movement.MoveTo(_lastTargetPos);
            }
            
            if (_targetLostTimer >= _targetLostThreshold)
                enemy.ChangeMovementState(enemy.ReturnMovementState);
            return;
        }

        // === 타겟 추적 ===
        _targetLostTimer = 0f;
        Vector3 currentTargetPos = enemy.CurrentTarget.position;
        _lastTargetPos = currentTargetPos;
        
        // [최적화] 거리 기반 동적 스로틀링
        float distToTarget = Vector3.Distance(enemy.transform.position, currentTargetPos);
        float dynamicInterval = GetIntervalByDistance(distToTarget);
        
        _moveTimer += Time.deltaTime;
        float distDiff = Vector3.SqrMagnitude(_lastMovedPos - currentTargetPos);
        
        // 동적 주기 또는 타겟이 0.5m 이상 이동했으면 즉시 갱신
        if (_moveTimer >= dynamicInterval || distDiff > 0.25f)
        {
            _moveTimer = 0f;
            _lastMovedPos = currentTargetPos;
            enemy.Movement.MoveTo(currentTargetPos);
        }
        
        // 이동 중이더라도 타겟을 계속 응시 (경로 막혔을 때 멍하니 있는 것 방지)
        enemy.Movement.FaceTarget(currentTargetPos);
        
        // 디버그 로그 (1초 간격)
        if (Time.time >= _nextLogTime)
        {
            _nextLogTime = Time.time + 1f;
            // Debug.Log($"[Chase] {enemy.name}: 추적 중... Target={enemy.CurrentTarget.name}, Pos={enemy.transform.position}");
        }
    }

    /// <summary>
    /// 상태 종료
    /// </summary>
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: ChaseState 종료");
    }
    
    /// <summary>
    /// 거리에 따른 경로 갱신 주기 반환
    /// 가까울수록 빠르게, 멀수록 느리게 갱신하여 연산 분산
    /// </summary>
    private float GetIntervalByDistance(float distance)
    {
        if (distance < CLOSE_RANGE)
            return CLOSE_INTERVAL; // 0~10m: 0.1초
        else if (distance < MID_RANGE)
            return MID_INTERVAL;   // 10~30m: 0.3초
        else
            return FAR_INTERVAL;   // 30m+: 0.6초
    }
}
