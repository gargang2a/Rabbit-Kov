using UnityEngine;
using UnityEngine.AI;

// 적 AI 총괄 컨트롤러 - 컴포넌트 조합 및 FSM 관리
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemySenses))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(NavMeshAgent))]

public class EnemyController : MonoBehaviour
{
    private EnemyStats _stats;             // 체력 관리
    private EnemyMovement _movement;       // 이동 시스템
    private EnemySenses _senses;           // 감지 시스템
    private EnemyCombat _combat;           // 전투 시스템
    private EnemyStateMachine _stateMachine; // 상태 기계 (FSM)
    private Transform _targetPlayer;       // 추적 대상

    private Collider _boundZone;           // 소속 Zone
    private bool _isPlayerInZone = false;  // 플레이어 Zone 진입 여부

    // 상태 객체 (재사용하여 GC 부하 방지)
    private IdleState _idleState = new IdleState();
    private PatrolState _patrolState = new PatrolState();
    private ChaseState _chaseState = new ChaseState();
    private AttackState _attackState = new AttackState();

    // 프로퍼티, 외부에서 읽기 전용
    public EnemyStats Stats => _stats;
    public EnemyMovement Movement => _movement;
    public EnemySenses Senses => _senses;
    public EnemyCombat Combat => _combat;
    public Transform CurrentTarget => _targetPlayer;
    public Collider BoundZone => _boundZone;
    public bool IsPlayerInZone => _isPlayerInZone;

    // 현재 상태 이름 (디버깅용)
    public string CurrentStateName
    {
        get
        {
            if (_stateMachine != null) return _stateMachine.CurrentStateName;
            return "Not Initialized";
        }
    }

    private void Awake()
    {
        CacheComponents();
        ChangeToIdle(); // 초기 상태: 대기
    }

    // 컴포넌트 캐싱 및 이벤트 연결
    protected virtual void CacheComponents()
    {
        _stats = GetComponent<EnemyStats>();
        _movement = GetComponent<EnemyMovement>();
        _senses = GetComponent<EnemySenses>();
        _combat = GetComponent<EnemyCombat>();
        _stateMachine = new EnemyStateMachine();

        // 사망 이벤트 연결
        if (_stats != null)
        {
            _stats.OnDeath += HandleDeath;
        }
    }

    private void Update()
    {
        if (_stats != null && _stats.IsDead) return; // 사망 시 종료
        _stateMachine?.Update(this); // 현재 상태 실행
    }

    private void OnDestroy()
    {
        // 사망 이벤트 해제 (메모리 누수 방지)
        if (_stats != null)
        {
            _stats.OnDeath -= HandleDeath;
        }
    }

    // Zone 할당 (스포너에서 호출)
    public void SetBoundZone(Collider zone)
    {
        _boundZone = zone;
        _movement?.SetBoundZone(zone); // 이동 제한도 같이 설정
    }

    // 플레이어 Zone 진입 시 호출 (스포너에서 호출)
    public void OnPlayerEnterZone(Transform player)
    {
        _isPlayerInZone = true;
        SetTarget(player);
    }

    // 플레이어 Zone 퇴장 시 호출 (스포너에서 호출)
    public void OnPlayerExitZone()
    {
        _isPlayerInZone = false;
        // 상태 변경은 각 State에서 IsPlayerInZone 체크로 처리
    }

    // 상태 전환 (외부에서 직접 State 객체 전달 시 사용)
    public void ChangeState(IEnemyState newState)
    {
        if (newState != null)
        {
            _stateMachine.ChangeState(newState, this);
        }
    }

    // 상태 전환 단축 메서드 (각 State에서 호출)
    // 미리 생성된 상태 객체를 재사용하여 GC 부하 방지
    public void ChangeToIdle() => _stateMachine.ChangeState(_idleState, this);     // 대기 상태
    public void ChangeToPatrol() => _stateMachine.ChangeState(_patrolState, this); // 정찰 상태
    public void ChangeToChase() => _stateMachine.ChangeState(_chaseState, this);   // 추격 상태
    public void ChangeToAttack() => _stateMachine.ChangeState(_attackState, this); // 공격 상태

    // 타겟 관리
    public void SetTarget(Transform target) => _targetPlayer = target;   // 타겟 설정
    public void ClearTarget() => _targetPlayer = null;                   // 타겟 해제
    public bool HasTarget() => _targetPlayer != null;                    // 타겟 존재 여부

    // 사망 처리
    protected virtual void HandleDeath()
    {
        _movement?.Stop(); // 이동 정지
        Debug.Log(gameObject.name + " 사망!");
    }
}
