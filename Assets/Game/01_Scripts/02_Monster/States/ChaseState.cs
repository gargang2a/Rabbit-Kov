using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// ChaseState - 추적 상태
// ============================================================================
// 
// [역할]
// 적이 플레이어를 발견하고 적극적으로 추적하는 상태입니다.
// 뛰기 속도로 플레이어를 향해 이동하며, 공격 사거리에 들어오면 AttackState로 전환합니다.
// 
// [상태 전환]
// - ChaseState → AttackState: 플레이어가 공격 사거리 내에 있을 때
// - ChaseState → InvestigateState: 플레이어를 시야에서 놓쳤을 때 (마지막 위치 전달)
// 
// [타르코프 스타일 AI 특징]
// - _lastKnownTargetPosition: 타겟을 마지막으로 본 위치를 매 프레임 갱신
// - 타겟을 놓치면 마지막 위치로 이동해서 조사 (현실적인 AI 행동)
// - 타겟이 갑자기 사라져도(파괴 등) 마지막 위치로 조사
// ============================================================================
public class ChaseState : IEnemyState
{
    // ==================== 멤버 변수 ====================
    
    // 타겟을 마지막으로 본 위치 (월드 좌표)
    // 매 프레임 타겟의 현재 위치로 갱신되며, 타겟을 놓쳤을 때 InvestigateState에 전달됩니다.
    // 이 값 덕분에 적이 "플레이어가 사라진 곳"으로 가서 조사하는 현실적인 행동을 합니다.
    private Vector3 _lastKnownTargetPosition;

    // ==================== IEnemyState 구현 ====================
    
    // 상태 진입 시 호출
    // 용도: 뛰기 속도 설정 및 타겟 초기 위치 기록
    public void Enter(EnemyController enemy)
    {
        // 뛰기 속도로 설정 (순찰 때보다 빠름)
        if (enemy.Movement != null)
        {
            enemy.Movement.SetRunSpeed();
        }

        // 진입 시 타겟 위치 기록 (null 체크 필수!)
        // 이 위치가 추적 실패 시 조사할 위치의 초기값이 됩니다.
        if (enemy.CurrentTarget != null)
        {
            _lastKnownTargetPosition = enemy.CurrentTarget.position;
        }
        else
        {
            // 드문 경우: 타겟 없이 ChaseState에 진입하면 현재 위치 사용
            _lastKnownTargetPosition = enemy.transform.position;
        }

        Debug.Log(enemy.gameObject.name + ": ChaseState 진입");
    }

    // 매 프레임 호출되는 메인 로직
    // 우선순위:
    // 1. 타겟이 공격 범위 내 → AttackState 전환
    // 2. 타겟을 향해 계속 이동
    // 3. 타겟을 시야에서 놓침 → InvestigateState 전환
    public void Execute(EnemyController enemy)
    {
        // ===== 예외 처리: Movement 없음 =====
        if (enemy.Movement == null)
        {
            enemy.ChangeState(new InvestigateState(_lastKnownTargetPosition));
            return;
        }

        // ===== 예외 처리: 타겟이 사라짐 (파괴됨 등) =====
        // 타겟이 null이면 마지막으로 본 위치로 조사하러 이동
        if (enemy.CurrentTarget == null)
        {
            enemy.ChangeState(new InvestigateState(_lastKnownTargetPosition));
            return;
        }

        // ===== 타겟 위치 갱신 =====
        // 타겟이 존재하면 매 프레임 마지막 위치 업데이트
        // 이렇게 해야 타겟을 놓쳤을 때 "가장 최근에 본 위치"로 갈 수 있음
        _lastKnownTargetPosition = enemy.CurrentTarget.position;

        // 거리 계산
        Vector3 myPosition = enemy.transform.position;
        Vector3 targetPosition = enemy.CurrentTarget.position;
        float distance = Vector3.Distance(myPosition, targetPosition);

        // ===== 1. 공격 사거리 체크 =====
        // Combat 컴포넌트가 있고, 타겟이 공격 범위 내에 있으면 공격 상태로 전환
        if (enemy.Combat != null && distance <= enemy.Combat.AttackRange)
        {
            enemy.ChangeState(new AttackState());
            return;
        }

        // ===== 2. 타겟을 향해 이동 =====
        // NavMeshAgent가 타겟 위치로 경로를 계산하고 이동
        enemy.Movement.MoveTo(targetPosition);

        // ===== 3. 시야 체크 =====
        // Senses.ScanForTarget()이 false를 반환하면 타겟을 시야에서 놓친 것
        // (장애물 뒤로 숨거나, 시야 범위를 벗어났거나)
        if (enemy.Senses != null && enemy.Senses.ScanForTarget() == false)
        {
            // 타겟을 놓침 → 마지막으로 본 위치로 조사하러 이동
            // InvestigateState 생성자에 _lastKnownTargetPosition 전달!
            enemy.ChangeState(new InvestigateState(_lastKnownTargetPosition));
            return;
        }
    }

    // 상태 종료 시 호출
    // 용도: 특별한 정리 작업 없음
    public void Exit(EnemyController enemy)
    {
        // ChaseState는 Exit에서 특별히 할 작업 없음
        Debug.Log(enemy.gameObject.name + ": ChaseState 종료");
    }
}
