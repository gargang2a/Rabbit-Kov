using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ============================================================================
// EnemyController - 적 AI 총괄 컨트롤러
// ============================================================================
// 
// [역할]
// 적의 모든 시스템을 통합하고 관리하는 핵심 컴포넌트입니다.
// 각 기능별 컴포넌트(Stats, Movement, Senses, Combat)를 조합(Composition)하여
// 하나의 완전한 적 AI를 구성합니다.
// 
// [컴포넌트 조합 패턴 (Composition over Inheritance)]
// - EnemyStats: 체력 관리
// - EnemyMovement: 이동 관리 (NavMeshAgent 래핑)
// - EnemySenses: 플레이어 감지 (시야각, 감지 범위)
// - EnemyCombat: 전투 관리 (공격 사거리, 쿨다운)
// - EnemyStateMachine: FSM 상태 관리
// 
// [주요 기능]
// 1. 컴포넌트 초기화 및 캐싱 (Awake)
// 2. 매 프레임 상태 기계 업데이트 (Update)
// 3. 타겟 관리 (SetTarget, ClearTarget, HasTarget)
// 4. 상태 전환 위임 (ChangeState)
// 5. 사망 처리 이벤트 구독 (HandleDeath)
// 
// [RequireComponent 사용 이유]
// - 이 컴포넌트가 필요로 하는 다른 컴포넌트들이 항상 존재하도록 보장합니다.
// - 인스펙터에서 EnemyController를 추가하면 자동으로 필수 컴포넌트도 추가됩니다.
// ============================================================================

// RequireComponent: 이 컴포넌트가 추가될 때 아래 컴포넌트들도 자동으로 추가됨
[RequireComponent(typeof(EnemyStats))]      // 체력 시스템
[RequireComponent(typeof(EnemyMovement))]   // 이동 시스템
[RequireComponent(typeof(EnemySenses))]     // 감지 시스템
[RequireComponent(typeof(EnemyCombat))]     // 전투 시스템
[RequireComponent(typeof(NavMeshAgent))]    // Unity 길찾기 시스템

public class EnemyController : MonoBehaviour
{
    // ==================== 컴포넌트 참조 (캐싱) ====================
    // GetComponent는 비용이 크므로 Awake에서 한 번만 호출하여 변수에 저장합니다.
    // 다른 스크립트에서는 프로퍼티를 통해 접근합니다.
    
    private EnemyStats _stats;              // 체력 관리 컴포넌트
    private EnemyMovement _movement;        // 이동 관리 컴포넌트
    private EnemySenses _senses;            // 플레이어 감지 컴포넌트
    private EnemyCombat _combat;            // 공격 관리 컴포넌트
    private EnemyStateMachine _stateMachine;  // FSM (상태 기계) - 일반 C# 클래스
    
    // 현재 추적 중인 타겟 (보통 플레이어의 Transform)
    // SetTarget()으로 설정, ClearTarget()으로 해제
    private Transform _currentTarget;

    // ==================== 프로퍼티 (외부 접근용) ====================
    // 각 State 클래스에서 enemy.Movement, enemy.Senses 등으로 접근합니다.
    
    public EnemyStats Stats { get { return _stats; } }
    public EnemyMovement Movement { get { return _movement; } }
    public EnemySenses Senses { get { return _senses; } }
    public EnemyCombat Combat { get { return _combat; } }
    public Transform CurrentTarget { get { return _currentTarget; } }

    // 현재 상태 이름 (디버깅용)
    // 예: "ChaseState", "PatrolState" 등
    public string CurrentStateName
    {
        get
        {
            if (_stateMachine != null) return _stateMachine.CurrentStateName;
            return "Not Initialized";
        }
    }

    // ==================== MonoBehaviour 생명주기 ====================
    
    // Awake: 가장 먼저 호출됨. 컴포넌트 초기화에 사용
    private void Awake()
    {
        Initialize();  // 초기화 함수 호출 (컴포넌트 캐싱, 이벤트 구독, 초기 상태 설정)
    }

    // 초기화 함수 (virtual로 선언하여 상속 클래스에서 오버라이드 가능)
    // protected: 이 클래스와 이 클래스를 상속받은 클래스에서만 접근 가능
    protected virtual void Initialize()
    {
        // 1. 컴포넌트 캐싱 (GetComponent 호출 최소화)
        _stats = GetComponent<EnemyStats>();
        _movement = GetComponent<EnemyMovement>();
        _senses = GetComponent<EnemySenses>();
        _combat = GetComponent<EnemyCombat>();

        // 2. 상태 기계 생성 (MonoBehaviour가 아니므로 new로 생성)
        _stateMachine = new EnemyStateMachine();

        // 3. 사망 이벤트 구독
        // EnemyStats.OnDeath 이벤트가 발생하면 HandleDeath 메서드 호출
        // 이벤트 패턴: 컴포넌트 간 느슨한 결합 (Loose Coupling)
        if (_stats != null)
        {
            _stats.OnDeath += HandleDeath;
        }

        // 4. 초기 상태 설정: IdleState로 시작
        _stateMachine.ChangeState(new IdleState(), this);
    }

    // Update: 매 프레임 호출됨. 상태 기계 실행
    private void Update()
    {
        // 이미 죽었으면 AI 로직 실행하지 않음
        if (_stats != null && _stats.isDead == true) return;

        // 상태 기계의 Update 호출 (현재 상태의 Execute 실행)
        if (_stateMachine != null)
        {
            _stateMachine.Update(this);
        }
    }

    // OnDestroy: 오브젝트 파괴 시 호출됨. 이벤트 구독 해제
    // 중요: 이벤트 구독을 해제하지 않으면 메모리 누수 발생 가능
    private void OnDestroy()
    {
        if (_stats != null)
        {
            _stats.OnDeath -= HandleDeath;
        }
    }

    // ==================== 공개 메서드 ====================
    
    // 상태 전환 요청. 각 State에서 enemy.ChangeState(new ChaseState()) 형태로 호출합니다.
    // 상태 기계에 상태 전환을 위임합니다.
    public void ChangeState(IEnemyState newState)
    {
        if (newState != null)
        {
            _stateMachine.ChangeState(newState, this);
        }
    }

    // 타겟 설정. EnemySenses에서 플레이어를 발견했을 때 호출합니다.
    // target: 추적할 대상의 Transform (보통 플레이어)
    public void SetTarget(Transform target)
    {  
        _currentTarget = target; 
    }

    // 타겟 해제. 플레이어를 놓쳤거나 조사를 완료했을 때 호출합니다.
    public void ClearTarget()
    { 
        _currentTarget = null; 
    }

    // 타겟 존재 여부 확인. 조건 체크에 사용합니다.
    // 반환값: 타겟이 있으면 true, 없으면 false
    public bool HasTarget()
    {
        return _currentTarget != null;
    }

    // ==================== 이벤트 핸들러 ====================
    
    // 사망 이벤트 핸들러. EnemyStats.OnDeath 이벤트 발생 시 호출됩니다.
    // virtual: 상속 클래스에서 오버라이드하여 추가 사망 로직 구현 가능
    // 예: 사망 애니메이션, 아이템 드롭, 경험치 지급 등
    protected virtual void HandleDeath()
    {
        // 사망 시 이동 정지
        if (_movement != null)
        {
            _movement.Stop();
        }

        Debug.Log(gameObject.name + " 사망!");
        
        // TODO: 추가 사망 처리
        // - 사망 애니메이션 재생
        // - 콜라이더 비활성화
        // - 일정 시간 후 오브젝트 파괴
        // - 아이템 드롭
    }
}

