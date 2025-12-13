using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// AttackState - 공격 상태
// ============================================================================
// 
// [역할]
// 적이 플레이어를 공격하는 상태입니다.
// 공격 사거리 내에서 플레이어를 바라보며 공격을 시도합니다.
// 
// [상태 전환]
// - AttackState → ChaseState: 플레이어가 공격 사거리를 벗어났을 때
// - AttackState → IdleState: 타겟이 사라졌을 때 (파괴됨 등)
// 
// [행동 패턴]
// 1. 제자리에 정지
// 2. 타겟 방향으로 회전 (부드럽게)
// 3. 공격 시도 (TryAttack - 쿨다운 체크 포함)
// 4. 타겟이 사거리를 벗어나면 다시 추적
// ============================================================================
public class AttackState : IEnemyState
{
    // ==================== IEnemyState 구현 ====================
    
    // 상태 진입 시 호출
    // 용도: 이동 정지 (제자리에서 공격하기 위해)
    public void Enter(EnemyController enemy)
    {
        // 공격 상태에서는 이동하지 않고 제자리에서 공격
        // Stop()을 호출하여 NavMeshAgent 이동 중지
        if (enemy.Movement != null)
        {
            enemy.Movement.Stop();
        }
        Debug.Log(enemy.gameObject.name + ": AttackState 진입");
    }

    // 매 프레임 호출되는 메인 로직
    // 1. 타겟 유효성 체크 → 없으면 IdleState
    // 2. 타겟 방향으로 회전
    // 3. 사거리 체크 → 벗어나면 ChaseState
    // 4. 공격 시도
    public void Execute(EnemyController enemy)
    {
        // ===== 1. 타겟 유효성 체크 =====
        // 타겟이 없으면 (파괴됨, null 등) 대기 상태로 전환
        if (enemy.CurrentTarget == null)
        {
            enemy.ChangeState(new IdleState());
            return;
        }

        // ===== 2. 타겟 방향으로 회전 =====
        // 공격 전에 타겟을 정면으로 바라봐야 함
        // LookAt()은 매 프레임 조금씩 회전 (_rotateSpeed에 따라)
        if (enemy.Movement != null)
        {
            enemy.Movement.LookAt(enemy.CurrentTarget);
        }

        // 거리 계산
        Vector3 myPosition = enemy.transform.position;
        Vector3 targetPosition = enemy.CurrentTarget.position;
        float distance = Vector3.Distance(myPosition, targetPosition);

        // ===== 3. 사거리 체크 =====
        // 타겟이 공격 범위를 벗어나면 다시 추적
        // Combat.AttackRange: EnemyCombat에서 설정된 공격 사거리
        if (enemy.Combat != null && distance > enemy.Combat.AttackRange)
        {
            enemy.ChangeState(new ChaseState());
            return;
        }

        // ===== 4. 공격 시도 =====
        // TryAttack(): 쿨다운이 완료되었으면 공격 실행, 아니면 대기
        // 실제 공격 로직 (데미지 처리 등)은 EnemyCombat에서 구현
        if (enemy.Combat != null)
        {
            enemy.Combat.TryAttack();
        }
    }

    // 상태 종료 시 호출
    // 용도: 특별한 정리 작업 없음 (디버그 로그만)
    public void Exit(EnemyController enemy)
    {
        Debug.Log(enemy.gameObject.name + ": AttackState 종료");
    }
}

