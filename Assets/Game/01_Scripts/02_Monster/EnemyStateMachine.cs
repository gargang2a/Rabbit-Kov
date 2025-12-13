using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// EnemyStateMachine - 적 AI 상태 기계 (FSM: Finite State Machine)
// ============================================================================
// 
// [역할]
// 적 AI의 상태를 관리하는 핵심 클래스입니다.
// 현재 상태를 저장하고, 상태 전환과 매 프레임 상태 실행을 담당합니다.
// 
// [왜 MonoBehaviour가 아닌가?]
// - EnemyStateMachine은 일반 C# 클래스입니다.
// - EnemyController가 이 클래스의 인스턴스를 생성하고 관리합니다.
// - MonoBehaviour가 아니어도 EnemyController.Update()에서 호출되므로 문제없습니다.
// 
// [상태 전환 흐름]
// ChangeState() 호출 시:
// 1. 현재 상태가 있으면 Exit() 호출 (정리 작업)
// 2. 새 상태를 현재 상태로 설정
// 3. 새 상태의 Enter() 호출 (초기화 작업)
// 4. 현재 상태 이름 업데이트 (디버깅용)
// 
// [주의사항]
// - ChangeState()는 즉시 상태를 교체합니다. 이전 상태의 Execute()가 남아있어도 실행되지 않습니다.
// - 상태 객체는 매번 new로 생성됩니다. 메모리를 절약하려면 상태 풀링을 고려하세요.
// ============================================================================
public class EnemyStateMachine
{
    // ==================== 멤버 변수 ====================
    
    // 현재 활성화된 상태 객체 (IEnemyState 인터페이스 타입)
    // IdleState, PatrolState, ChaseState, AttackState, InvestigateState 중 하나가 들어갑니다.
    private IEnemyState _currentState;
    
    // 현재 상태의 클래스 이름 (디버깅 및 UI 표시용)
    // GetType().Name으로 "ChaseState" 같은 문자열을 저장합니다.
    private string _currentStateName = "None";

    // ==================== 프로퍼티 ====================
    
    // 현재 상태 이름을 외부에서 읽을 수 있게 하는 프로퍼티
    // EnemyController.CurrentStateName에서 이 값을 반환합니다.
    // 디버깅 시 Scene 뷰나 UI에서 현재 상태를 표시할 때 유용합니다.
    public string CurrentStateName { get { return _currentStateName; } }

    // ==================== 핵심 메서드 ====================
    
    // 상태를 전환하는 함수. 적 AI의 행동 모드를 바꿀 때 호출됩니다.
    // newState: 전환할 새로운 상태 객체 (예: new ChaseState())
    // enemy: 이 상태 기계를 소유한 EnemyController 참조
    // 
    // [호출 순서]
    // 1. 현재 상태 Exit() → 2. 새 상태 저장 → 3. 새 상태 Enter()
    public void ChangeState(IEnemyState newState, EnemyController enemy)
    {
        // ===== 1. 현재 상태 종료 =====
        // 현재 상태가 존재하면 Exit() 호출하여 정리 작업 수행
        // 예: 타이머 리셋, 플래그 초기화 등
        if (_currentState != null)
        {
            _currentState.Exit(enemy);
        }

        // ===== 2. 새 상태 저장 =====
        // 현재 상태를 새 상태로 교체
        _currentState = newState;

        // ===== 3. 새 상태 진입 =====
        if (_currentState != null)
        {
            // 새 상태의 Enter() 호출하여 초기화 작업 수행
            // 예: 이동 속도 설정, 목적지 설정 등
            _currentState.Enter(enemy);

            // 상태 이름 저장 (디버깅용)
            // GetType().Name: 클래스 이름을 문자열로 반환
            // 예: typeof(ChaseState).Name → "ChaseState"
            _currentStateName = _currentState.GetType().Name;
        }
        else
        {
            // 새 상태가 null이면 상태 없음 표시
            _currentStateName = "None";
        }
    }

    // 현재 상태를 실행하는 함수. EnemyController.Update()에서 매 프레임 호출됩니다.
    // enemy: 이 상태 기계를 소유한 EnemyController 참조
    // 
    // [중요]
    // - MonoBehaviour.Update()에서 호출되므로 매 프레임 실행됩니다.
    // - 현재 상태가 null이면 아무것도 하지 않습니다.
    public void Update(EnemyController enemy)
    {
        // 현재 상태가 있으면 Execute() 호출
        // Execute()에서 상태별 메인 로직이 실행됩니다.
        // 예: PatrolState.Execute()에서 이동, 플레이어 감지 등
        if (_currentState != null)
        {
            _currentState.Execute(enemy);
        }
    }
}

