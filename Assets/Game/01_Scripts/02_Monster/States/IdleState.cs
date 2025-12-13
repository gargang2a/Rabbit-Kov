using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// IdleState - 대기 상태
// ============================================================================
// 
// [역할]
// 적이 아무것도 하지 않고 제자리에서 대기하는 상태입니다.
// 게임 시작 시 또는 타겟을 완전히 잃었을 때 진입합니다.
// 
// [상태 전환]
// - IdleState → ChaseState: 플레이어를 감지했을 때
// - IdleState → PatrolState: 대기 시간(_idleDuration)이 지나면 순찰 시작
// 
// [사용 예시]
// - 게임 시작 시 적의 초기 상태로 사용
// - InvestigateState에서 플레이어를 찾지 못했을 때 전환
// ============================================================================
public class IdleState : IEnemyState
{
    // ==================== 멤버 변수 ====================
    
    // 현재 대기한 시간을 누적하는 타이머 (초 단위)
    // Execute()에서 Time.deltaTime을 계속 더해서 경과 시간을 측정합니다.
    private float _idleTime = 0f;
    
    // 대기 상태를 유지할 총 시간 (초 단위)
    // 이 시간이 지나면 PatrolState로 전환됩니다.
    // 기획에서 조절 가능: 짧으면 적이 빨리 움직이고, 길면 느긋해 보임
    private float _idleDuration = 1f;

    // ==================== IEnemyState 구현 ====================
    
    // 상태 진입 시 호출
    // 용도: 이동 정지 및 타이머 초기화
    public void Enter(EnemyController enemy)
    {
        // Movement 컴포넌트가 있으면 즉시 정지
        // 이전 상태에서 이동 중이었을 수 있으므로 확실히 멈춥니다.
        if (enemy.Movement != null)
        {
            enemy.Movement.Stop();
        }

        // 타이머 초기화: 새로운 대기 시간 측정 시작
        _idleTime = 0f;

        Debug.Log(enemy.gameObject.name + ": IdleState 진입");
    }

    // 매 프레임 호출되는 메인 로직
    // 1. 플레이어 감지 체크 → ChaseState 전환
    // 2. 대기 시간 경과 체크 → PatrolState 전환
    public void Execute(EnemyController enemy)
    {
        // [우선순위 1] 플레이어 감지 체크
        // Senses 컴포넌트의 ScanForTarget()이 true를 반환하면 플레이어 발견
        // 즉시 ChaseState로 전환하여 추적 시작
        if (enemy.Senses != null && enemy.Senses.ScanForTarget())
        {
            enemy.ChangeState(new ChaseState());
            // return이 없어도 ChangeState 후에는 이 Execute가 더 이상 호출되지 않음
            // 하지만 명시적으로 return하는 것이 의도를 명확하게 함
        }

        // 대기 시간 누적
        // Time.deltaTime: 이전 프레임부터 현재 프레임까지 경과한 시간 (초)
        _idleTime += Time.deltaTime;

        // [우선순위 2] 대기 시간 완료 체크
        // _idleDuration초 이상 대기했으면 순찰 상태로 전환
        if (_idleTime >= _idleDuration)
        {
            // 순찰이 가능한 상태인지 확인 (Movement가 있고 CanPatrol이 true)
            // CanPatrol(): 랜덤 순찰 모드가 활성화되어 있는지 확인
            if (enemy.Movement != null && enemy.Movement.CanPatrol())
            {
                enemy.ChangeState(new PatrolState());
                return;
            }
            // 순찰이 불가능하면 계속 대기 (무한 대기 상태)
        }
    }

    // 상태 종료 시 호출
    // 용도: 특별한 정리 작업 없음 (디버그 로그만)
    public void Exit(EnemyController enemy)
    {
        Debug.Log(enemy.gameObject.name + ": IdleState 종료");
    }
}

