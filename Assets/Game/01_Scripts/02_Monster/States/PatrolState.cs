using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// PatrolState - 순찰 상태
// ============================================================================
// 
// [역할]
// 적이 맵을 랜덤하게 돌아다니며 순찰하는 상태입니다.
// NavMesh 위의 랜덤한 위치로 이동 → 도착 → 주변 둘러보기 → 다시 랜덤 이동을 반복합니다.
// ============================================================================
public class PatrolState : IEnemyState
{
    // ==================== 멤버 변수 ====================
    
    // 주변 둘러보기 타이머
    private float _lookAroundTimer = 0f;
    
    // 둘러보는 총 시간 (3초)
    private float _lookAroundDuration = 3f;
    
    // 회전 관련
    private bool _hasReachedLookDirection = true;
    private Vector3 _currentLookTarget;
    private float _lookChangeInterval = 0.8f;
    private float _lastLookChangeTime = 0f;
    
    // 둘러보기 중 여부
    private bool _isLookingAround = false;
    
    // 이동 시작 후 경과 시간 (도착 오판정 방지)
    private float _moveStartTime = 0f;
    private float _minMoveTime = 0.5f; // 최소 0.5초는 이동해야 도착 체크

    // ==================== IEnemyState 구현 ====================
    
    public void Enter(EnemyController enemy)
    {
        _isLookingAround = false;
        _lookAroundTimer = 0f;
        _hasReachedLookDirection = true;
        _moveStartTime = Time.time;

        if (enemy.Movement == null) return;

        if (enemy.Movement.UseRandomPatrol)
        {
            enemy.Movement.ForceRandomMove();
        }
        Debug.Log(enemy.gameObject.name + ": PatrolState 진입");
    }

    public void Execute(EnemyController enemy)
    {
        // ===== 1. 플레이어 감지 체크 (최우선) =====
        if (enemy.Senses != null && enemy.Senses.ScanForTarget())
        {
            enemy.ChangeState(new ChaseState());
            return;
        }

        if (enemy.Movement == null) return;

        // ===== 2. 주변 둘러보기 모드 (3초) =====
        if (_isLookingAround)
        {
            _lookAroundTimer += Time.deltaTime;

            // 일정 간격마다 새로운 방향
            if (Time.time - _lastLookChangeTime >= _lookChangeInterval)
            {
                GenerateNewLookDirection(enemy);
                _lastLookChangeTime = Time.time;
                _hasReachedLookDirection = false;
            }

            // 목표 방향으로 회전
            if (!_hasReachedLookDirection)
            {
                _hasReachedLookDirection = enemy.Movement.LookAt(_currentLookTarget);
            }

            // 둘러보기 완료 → 다음 목적지로 이동
            if (_lookAroundTimer >= _lookAroundDuration)
            {
                _isLookingAround = false;
                _lookAroundTimer = 0f;
                _moveStartTime = Time.time; // 이동 시작 시간 기록

                if (enemy.Movement.UseRandomPatrol)
                {
                    enemy.Movement.MoveToRandomPoint();
                }
            }

            return;
        }

        // ===== 3. 이동 중 - 도착 확인 =====
        // 최소 이동 시간이 지나야 도착 체크 (오판정 방지)
        if (Time.time - _moveStartTime < _minMoveTime)
        {
            return; // 아직 이동 시작 직후
        }

        if (enemy.Movement.HasReacheddestination)
        {
            // 도착 → 둘러보기 모드
            _isLookingAround = true;
            _lookAroundTimer = 0f;
            _lastLookChangeTime = Time.time;
            
            enemy.Movement.Stop();
            
            GenerateNewLookDirection(enemy);
            _hasReachedLookDirection = false;
        }
    }

    public void Exit(EnemyController enemy)
    {
        Debug.Log(enemy.gameObject.name + ": PatrolState 종료");        
    }

    // ==================== 헬퍼 메서드 ====================
    
    private void GenerateNewLookDirection(EnemyController enemy)
    {
        float randomAngle = Random.Range(-120f, 120f);
        Vector3 randomDirection = Quaternion.Euler(0, randomAngle, 0) * enemy.transform.forward;
        _currentLookTarget = enemy.transform.position + randomDirection * 5f;
    }
}
