using UnityEngine;

// [역할] 추적 상태 - 타겟을 향해 이동
public class ChaseState : IMovementState
{
    private Vector3 _lastTargetPos;               // 마지막 타겟 위치
    private float _targetLostTimer = 0f;          // 타겟 소실 타이머
    private float _targetLostThreshold = 3f;      // 복귀까지 대기 시간
    private float _nextLogTime;                   // 디버그 로그 타이머
    private float _moveTimer = 0f;                // 이동 명령 타이머
    
    // 거리 기반 동적 스로틀링
    private const float CLOSE_RANGE = 10f;        // 근접 거리
    private const float MID_RANGE = 30f;          // 중거리
    private const float CLOSE_INTERVAL = 0.1f;    // 근접: 0.1초
    private const float MID_INTERVAL = 0.3f;      // 중거리: 0.3초
    private const float FAR_INTERVAL = 0.6f;      // 원거리: 0.6초
    
    private Vector3 _lastMovedPos;                // 마지막 이동 명령 위치

    // 상태 진입
    public void Enter(EnemyController enemy)
    {
        enemy.Movement?.SetChaseSpeed();
        _targetLostTimer = 0f;
        _moveTimer = Random.Range(0f, FAR_INTERVAL); // 시간 분산

        if (enemy.CurrentTarget != null)
        {
            _lastMovedPos = enemy.CurrentTarget.position;
        }
        else
        {
            _lastMovedPos = enemy.transform.position;
        }
            
        _lastTargetPos = _lastMovedPos;

        Debug.Log($"{enemy.gameObject.name}: ChaseState 진입");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        if (enemy.Movement == null) return;

        // Normal: 타겟 없으면 재검색
        if (enemy.CurrentTarget == null && !enemy.RestrictToZone)
        {
            Debug.LogWarning($"[Chase] {enemy.name}: Normal - 타겟 없음! 플레이어 재검색...");
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                enemy.SetTarget(player.transform);
                Debug.Log($"[Chase] {enemy.name}: 타겟 재할당 -> {player.name}");
            }
            else
            {
                Debug.LogError($"[Chase] {enemy.name}: 플레이어를 찾을 수 없음!");
            }
        }

        // 타겟 소실 처리
        if (enemy.CurrentTarget == null)
        {
            _targetLostTimer += Time.deltaTime;
            
            _moveTimer += Time.deltaTime;
            if (_moveTimer >= FAR_INTERVAL) // 마지막 위치로 이동
            {
                _moveTimer = 0f;
                enemy.Movement.MoveTo(_lastTargetPos);
            }
            
            if (_targetLostTimer >= _targetLostThreshold) // 복귀
            {
                enemy.ChangeMovementState(enemy.ReturnMovementState);
            }
            return;
        }

        // 타겟 추적
        _targetLostTimer = 0f;
        Vector3 currentTargetPos = enemy.CurrentTarget.position;
        _lastTargetPos = currentTargetPos;
        
        // 거리 기반 동적 스로틀링
        float distToTarget = Vector3.Distance(enemy.transform.position, currentTargetPos);
        float dynamicInterval = GetIntervalByDistance(distToTarget);
        
        _moveTimer += Time.deltaTime;
        float distDiff = Vector3.SqrMagnitude(_lastMovedPos - currentTargetPos);
        
        // 주기 또는 타겟 이동 시 갱신
        if (_moveTimer >= dynamicInterval || distDiff > 0.25f)
        {
            _moveTimer = 0f;
            _lastMovedPos = currentTargetPos;
            enemy.Movement.MoveTo(currentTargetPos);
        }
        
        // [수정됨] NavMeshAgent.updateRotation = true 이므로 자동 회전
        // FaceTarget()은 공격 시에만 호출 (CombatState에서 처리)
        // enemy.Movement.FaceTarget(currentTargetPos);
        
        // 디버그 (1초 간격)
        if (Time.time >= _nextLogTime)
        {
            _nextLogTime = Time.time + 1f;
        }
    }

    // 상태 종료
    public void Exit(EnemyController enemy)
    {
        Debug.Log($"{enemy.gameObject.name}: ChaseState 종료");
    }
    
    // 거리 기반 경로 갱신 주기
    private float GetIntervalByDistance(float distance)
    {
        if (distance < CLOSE_RANGE)
        {
            return CLOSE_INTERVAL; // 근접
        }
        else if (distance < MID_RANGE)
        {
            return MID_INTERVAL;   // 중거리
        }
        else
        {
            return FAR_INTERVAL;   // 원거리
        }
    }
}
