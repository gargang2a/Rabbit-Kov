using UnityEngine;
using UnityEngine.AI;

// 조사 상태 - 마지막으로 플레이어를 본 위치 주변 조사
public class InvestigateState : IEnemyState
{
    private Vector3 _investigatePosition;             // 조사할 위치 (월드 좌표)
    private bool _hasCustomPosition = false;          // 생성자에서 위치를 받았는지 여부
    private bool _isLookingAround = false;            // 주변 관찰 중 여부
    private float _lookAroundTimer = 0f;              // 관찰 경과 시간
    private float _lookAroundDuration = 3f;           // 관찰 총 시간 (초)

    private float _moveTimer = 0f;                    // 이동 경과 시간
    private float _moveTimeout = 5f;                  // 이동 포기 시간 (초)
    private float _investigateRadius = 10f;           // 조사 위치 분산 반경 (m)

    private float _stateEnterCooldown = 1f;           // 감지 무시 시간 (무한 루프 방지)
    private float _timeSinceEnter = 0f;               // 상태 진입 후 경과 시간

    // 생성자: 기본
    public InvestigateState() { _hasCustomPosition = false; }

    // 생성자: 조사 위치 지정
    public InvestigateState(Vector3 lastKnownPosition)
    {
        _investigatePosition = lastKnownPosition;
        _hasCustomPosition = true;
    }

    // 상태 진입: 조사 위치 결정 및 이동 시작
    public void Enter(EnemyController enemy)
    {
        // 조사 기준 위치 결정
        Vector3 basePosition;

        if (_hasCustomPosition)
            basePosition = _investigatePosition;
        else if (enemy.CurrentTarget != null)
            basePosition = enemy.CurrentTarget.position;
        else
            basePosition = enemy.transform.position;

        // 기준 위치 주변 랜덤 위치로 이동 (적들이 한 곳에 몰리는 것 방지)
        _investigatePosition = GetRandomPointNear(basePosition, _investigateRadius);

        // 타이머 초기화
        _isLookingAround = false;
        _lookAroundTimer = 0f;
        _moveTimer = 0f;
        _timeSinceEnter = 0f;

        // 빠르게 조사 위치로 이동
        if (enemy.Movement != null)
        {
            enemy.Movement.SetChaseSpeed();
            enemy.Movement.MoveTo(_investigatePosition);
        }

        Debug.Log(enemy.gameObject.name + ": InvestigateState 진입");
    }

    // 매 프레임 실행
    public void Execute(EnemyController enemy)
    {
        if (enemy.Movement == null) return;

        _timeSinceEnter += Time.deltaTime;

        // === 1단계: 조사 위치로 이동 중 ===
        if (_isLookingAround == false)
        {
            _moveTimer += Time.deltaTime;

            // 이동 타임아웃 (5초) 시 순찰로 복귀
            if (_moveTimer >= _moveTimeout)
            {
                enemy.ClearTarget();
                enemy.ChangeState(new PatrolState());
                return;
            }

            if (_timeSinceEnter >= _stateEnterCooldown)
            {
                if (enemy.HasTarget())
                {
                    enemy.ChangeState(new ChaseState());
                    return;
                }
            }

            // 도착 시 관찰 모드로 전환
            if (enemy.Movement.HasReachedDestination)
            {
                enemy.Movement.Stop();
                _isLookingAround = true;
                _lookAroundTimer = 0f;
            }
            return;
        }

        // === 2단계: 주변 관찰 중 (3초) ===
        _lookAroundTimer += Time.deltaTime;

        if (_timeSinceEnter >= _stateEnterCooldown)
        {
            if (enemy.HasTarget())
            {
                enemy.ChangeState(new ChaseState());
                return;
            }
        }

        // 관찰 완료 시 순찰로 복귀
        if (_lookAroundTimer >= _lookAroundDuration)
        {
            enemy.ClearTarget();
            enemy.ChangeState(new PatrolState());
        }
    }

    // 상태 종료: 타이머 초기화
    public void Exit(EnemyController enemy)
    {
        _isLookingAround = false;
        _lookAroundTimer = 0f;
        _moveTimer = 0f;
        _timeSinceEnter = 0f;
        Debug.Log(enemy.gameObject.name + ": InvestigateState 종료");
    }

    // 기준 위치 주변 랜덤 NavMesh 위치 탐색
    private Vector3 GetRandomPointNear(Vector3 centerPosition, float radius)
    {
        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            // 원형 범위 내 랜덤 오프셋
            Vector2 randomOffset = Random.insideUnitCircle * radius;
            Vector3 randomPosition = centerPosition + new Vector3(randomOffset.x, 0, randomOffset.y);

            // NavMesh 위 유효 위치 탐색
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPosition, out hit, 5f, NavMesh.AllAreas))
            {
                Vector3 result = hit.position;
                result.y = centerPosition.y; // 높이 고정
                return result;
            }
        }

        return centerPosition; // 실패 시 원래 위치 반환
    }
}
