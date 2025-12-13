using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ============================================================================
// InvestigateState - 조사 상태
// ============================================================================
// 
// [역할]
// 적이 플레이어를 시야에서 놓친 후, 마지막으로 본 위치 주변으로 가서 조사하는 상태입니다.
// 정확한 위치가 아닌 주변 랜덤 위치로 이동하여 더 자연스러운 AI 행동을 구현합니다.
// 
// [상태 전환]
// - InvestigateState → ChaseState: 조사 중 플레이어를 다시 발견했을 때
// - InvestigateState → PatrolState: 조사 완료 또는 이동 타임아웃 시
// 
// [조사 흐름]
// 1. 마지막으로 플레이어를 본 위치 주변(반경 3~5m)의 랜덤 위치로 이동
// 2. 이동 중 타임아웃(10초) 발생 시 → PatrolState로 복귀
// 3. 도착 후 제자리에서 주변 관찰 (3초)
// 4. 관찰 중 플레이어 발견 → ChaseState
// 5. 관찰 완료 후 플레이어 못 찾음 → PatrolState
// ============================================================================
public class InvestigateState : IEnemyState
{
    // ==================== 멤버 변수 ====================
    
    // 조사할 위치 (월드 좌표)
    private Vector3 _investigatePosition;
    
    // 생성자에서 위치를 직접 받았는지 여부
    private bool _hasCustomPosition = false;
    
    // 현재 "주변 관찰 중" 상태인지 여부
    private bool _isLookingAround = false;
    
    // 주변 관찰 경과 시간 (초 단위)
    private float _lookAroundTimer = 0f;
    
    // 주변 관찰 총 시간 (초 단위)
    private float _lookAroundDuration = 3f;

    // 이동 타임아웃 관련 변수
    private float _moveTimer = 0f; // 이동 경과 시간
    private float _moveTimeout = 5f; // 이동 타임아웃 (5초)
    
    // 주변 위치로 이동할 때의 랜덤 반경
    // 값이 클수록 여러 적이 같은 위치로 몰리지 않고 분산됨
    private float _investigateRadius = 10f; // 10m 범위로 분산

    // 상태 전환 쿨다운 (무한 루프 방지)
    // 진입 직후 일정 시간 동안 플레이어 감지를 무시하여
    // InvestigateState ↔ ChaseState 무한 전환 방지
    private float _stateEnterCooldown = 1f; // 쿨다운 시간 (1초)
    private float _timeSinceEnter = 0f; // 진입 후 경과 시간

    // ==================== 생성자 ====================
    
    // 기본 생성자
    public InvestigateState()
    {
        _hasCustomPosition = false;
    }

    // 위치 지정 생성자
    public InvestigateState(Vector3 lastKnownPosition)
    {
        _investigatePosition = lastKnownPosition;
        _hasCustomPosition = true;
    }

    // ==================== IEnemyState 구현 ====================
    
    // 상태 진입 시 호출
    public void Enter(EnemyController enemy)
    {
        // ===== 조사 위치 결정 =====
        Vector3 basePosition;
        
        if (_hasCustomPosition)
        {
            basePosition = _investigatePosition;
        }
        else if (enemy.CurrentTarget != null)
        {
            basePosition = enemy.CurrentTarget.position;
        }
        else
        {
            basePosition = enemy.transform.position;
        }

        // 정확한 위치가 아닌 주변 랜덤 위치로 이동
        // 여러 적이 같은 위치로 몰리는 것을 방지
        _investigatePosition = GetNearbyPosition(basePosition, _investigateRadius);

        // 상태 플래그 초기화
        _isLookingAround = false;
        _lookAroundTimer = 0f;
        _moveTimer = 0f;
        _timeSinceEnter = 0f; // 쿨다운 타이머 초기화

        // 조사 위치로 빠르게 이동 (뛰기 속도)
        if (enemy.Movement != null)
        {
            enemy.Movement.SetRunSpeed();
            enemy.Movement.MoveTo(_investigatePosition);
        }
        Debug.Log(enemy.gameObject.name + ": InvestigateState 진입");
    }

    // 매 프레임 호출
    public void Execute(EnemyController enemy)
    {
        if (enemy.Movement == null) return;

        // 쿨다운 타이머 업데이트
        _timeSinceEnter += Time.deltaTime;

        // ===== 1단계: 조사 위치로 이동 중 =====
        if (_isLookingAround == false)
        {
            // 이동 타이머 업데이트
            _moveTimer += Time.deltaTime;

            // 이동 타임아웃 체크 - 너무 오래 걸리면 포기하고 순찰로 복귀
            if (_moveTimer >= _moveTimeout)
            {
                Debug.Log(enemy.gameObject.name + ": InvestigateState 이동 타임아웃 - PatrolState로 복귀");
                enemy.ClearTarget();
                enemy.ChangeState(new PatrolState());
                return;
            }

            // 쿨다운 중에는 플레이어 감지 무시 (무한 루프 방지)
            // 1초 후부터 플레이어 감지 시작
            if (_timeSinceEnter >= _stateEnterCooldown)
            {
                // 이동 중에도 플레이어 감지 체크
                if (enemy.Senses != null && enemy.Senses.ScanForTarget())
                {
                    enemy.ChangeState(new ChaseState());
                    return;
                }
            }

            // 목적지 도착 여부 확인
            if (enemy.Movement.HasReacheddestination)
            {
                enemy.Movement.Stop();
                _isLookingAround = true;
                _lookAroundTimer = 0f;
            }
            return;
        }

        // ===== 2단계: 주변 관찰 중 =====
        _lookAroundTimer += Time.deltaTime;

        // 쿨다운 후에만 플레이어 감지 (무한 루프 방지)
        if (_timeSinceEnter >= _stateEnterCooldown)
        {
            if (enemy.Senses != null && enemy.Senses.ScanForTarget())
            {
                enemy.ChangeState(new ChaseState());
                return;
            }
        }

        // 관찰 시간 완료 체크
        if (_lookAroundTimer >= _lookAroundDuration)
        {
            enemy.ClearTarget();
            enemy.ChangeState(new PatrolState());
            return;
        }
    }

    // 상태 종료 시 호출
    public void Exit(EnemyController enemy)
    {
        _isLookingAround = false;
        _lookAroundTimer = 0f;
        _moveTimer = 0f;
        _timeSinceEnter = 0f;

        Debug.Log(enemy.gameObject.name + ": InvestigateState 종료");
    }

    // ==================== 헬퍼 메서드 ====================
    
    // 기준 위치 주변의 랜덤한 NavMesh 위치 반환
    // 여러 적이 같은 위치로 몰리는 것을 방지
    private Vector3 GetNearbyPosition(Vector3 centerPosition, float radius)
    {
        int maxAttempts = 10;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            // 랜덤 오프셋 생성 (원형 범위)
            Vector2 randomOffset = Random.insideUnitCircle * radius;
            Vector3 randomPosition = centerPosition + new Vector3(randomOffset.x, 0, randomOffset.y);

            // NavMesh 위의 유효한 위치 찾기
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPosition, out hit, 5f, NavMesh.AllAreas))
            {
                // Y좌표를 현재 높이로 고정 (다른 층으로 이동 방지)
                Vector3 result = hit.position;
                result.y = centerPosition.y;
                return result;
            }
        }

        // 실패 시 원래 위치 반환
        return centerPosition;
    }
}
