using UnityEngine;
using UnityEngine.AI;

// [역할] 적 AI 총괄 컨트롤러 - 병렬 FSM 관리 (이동 + 전투)
// 리팩토링: 단일 FSM → MovementFSM + CombatFSM 병렬 실행
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemySenses))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("몬스터 유형")]
    [Tooltip("true: 에픽 몬스터 (순찰/수색), false: 일반 몬스터 (돌진)")]
    [SerializeField] private bool _isEpic = false;

    // === 컴포넌트 참조 ===
    private EnemyStats _stats;
    private EnemyMovement _movement;
    private EnemySenses _senses;
    private EnemyCombat _combat;
    private Transform _targetPlayer;

    // === 병렬 FSM ===
    private MovementFSM _movementFSM;
    private CombatFSM _combatFSM;

    // === 이동 상태 객체 (재사용으로 GC 방지) ===
    private PatrolState _patrolState = new PatrolState();
    private ChaseState _chaseState = new ChaseState();
    private StoppedState _stoppedState = new StoppedState();
    private ReturnState _returnState = new ReturnState();
    private WaitState _waitState = new WaitState(); // Normal 몬스터 전용

    // === 전투 상태 객체 ===
    private CombatInactiveState _combatInactiveState = new CombatInactiveState();
    private CombatReadyState _combatReadyState = new CombatReadyState();
    private CombatWindupState _combatWindupState = new CombatWindupState();
    private CombatAttackingState _combatAttackingState = new CombatAttackingState();
    private CombatRecoveryState _combatRecoveryState = new CombatRecoveryState();

    // Zone 관련
    private Collider[] _boundZones;
    private bool _isPlayerInZone = false;

    // === 프로퍼티 (컴포넌트 접근) ===
    public EnemyStats Stats => _stats;
    public EnemyMovement Movement => _movement;
    public EnemySenses Senses => _senses;
    public EnemyCombat Combat => _combat;
    public Transform CurrentTarget => _targetPlayer;
    public Collider[] BoundZones => _boundZones;
    public bool IsPlayerInZone => _isPlayerInZone;
    public bool IsEpic => _isEpic;

    // === 프로퍼티 (이동 상태 접근) ===
    public IMovementState PatrolMovementState => _patrolState;
    public IMovementState ChaseMovementState => _chaseState;
    public IMovementState StoppedMovementState => _stoppedState;
    public IMovementState ReturnMovementState => _returnState;
    public IMovementState WaitMovementState => _waitState; // Normal 전용

    // === 프로퍼티 (전투 상태 접근) ===
    public ICombatState CombatInactiveState => _combatInactiveState;
    public ICombatState CombatReadyState => _combatReadyState;
    public ICombatState CombatWindupState => _combatWindupState;
    public ICombatState CombatAttackingState => _combatAttackingState;
    public ICombatState CombatRecoveryState => _combatRecoveryState;

    // === 디버깅용 현재 상태 이름 ===
    public string CurrentMovementStateName => _movementFSM?.CurrentStateName ?? "None";
    public string CurrentCombatStateName => _combatFSM?.CurrentStateName ?? "None";

    private void Awake()
    {
        CacheComponents();
        InitializeFSMs();
    }

    /// <summary>
    /// 컴포넌트 캐싱 및 이벤트 연결
    /// </summary>
    protected virtual void CacheComponents()
    {
        _stats = GetComponent<EnemyStats>();
        _movement = GetComponent<EnemyMovement>();
        _senses = GetComponent<EnemySenses>();
        _combat = GetComponent<EnemyCombat>();

        // 사망 이벤트 연결
        if (_stats != null)
        {
            _stats.OnDeath += HandleDeath;
        }
    }

    /// <summary>
    /// FSM 초기화 및 시작 상태 설정
    /// </summary>
    private void InitializeFSMs()
    {
        _movementFSM = new MovementFSM();
        _combatFSM = new CombatFSM();

        // 이동 FSM 시작 상태: 유형별 분기
        if (_isEpic)
        {
            // Epic: Patrol (순찰하며 탐색)
            _movementFSM.ChangeState(_patrolState, this);
        }
        else
        {
            // Normal: Wait (대기 → Zone 진입 시 돌진)
            _movementFSM.ChangeState(_waitState, this);
        }
        
        // 전투 FSM 시작 상태: Inactive (타겟 없음)
        _combatFSM.ChangeState(_combatInactiveState, this);
    }

    private void Update()
    {
        // 사망 시 모든 FSM 정지
        if (_stats != null && _stats.IsDead) return;

        // === 병렬 FSM 실행 ===
        _movementFSM?.Update(this);
        _combatFSM?.Update(this);
    }

    private void OnDestroy()
    {
        if (_stats != null)
        {
            _stats.OnDeath -= HandleDeath;
        }
    }

    // ========== Zone 관리 ==========

    public void SetBoundZones(Collider[] zones)
    {
        _boundZones = zones;
        _movement?.SetBoundZones(zones);
    }

    public void SetBoundZone(Collider zone)
    {
        _boundZones = zone != null ? new Collider[] { zone } : null;
        _movement?.SetBoundZones(_boundZones);
    }

    public void OnPlayerEnterZone(Transform player)
    {
        _isPlayerInZone = true;
        SetTarget(player);
        
        if (!_isEpic)
        {
            // 일반 몬스터: 즉시 추격
            ChangeMovementState(_chaseState);
        }
    }

    public void OnPlayerExitZone()
    {
        _isPlayerInZone = false;
    }

    // ========== 이동 FSM 상태 전환 ==========

    /// <summary>
    /// 이동 상태 전환
    /// </summary>
    public void ChangeMovementState(IMovementState newState)
    {
        _movementFSM?.ChangeState(newState, this);
    }

    // ========== 전투 FSM 상태 전환 ==========

    /// <summary>
    /// 전투 상태 전환
    /// </summary>
    public void ChangeCombatState(ICombatState newState)
    {
        _combatFSM?.ChangeState(newState, this);
    }

    // ========== 이동 잠금 (FSM 간 통신) ==========

    /// <summary>
    /// 이동 잠금 (정지 공격 시 CombatFSM에서 호출)
    /// </summary>
    public void LockMovement()
    {
        _movementFSM?.Lock(this, _stoppedState);
    }

    /// <summary>
    /// 이동 잠금 해제 (공격 완료 시 CombatFSM에서 호출)
    /// </summary>
    public void UnlockMovement()
    {
        _movementFSM?.Unlock(this);
    }

    /// <summary>
    /// 이동 잠금 상태 확인
    /// </summary>
    public bool IsMovementLocked => _movementFSM?.IsLocked ?? false;

    // ========== 타겟 관리 ==========

    public void SetTarget(Transform target)
    {
        _targetPlayer = target;
    }

    public void ClearTarget()
    {
        _targetPlayer = null;
        // 타겟 소실 시 전투 FSM 리셋
        _combatFSM?.Reset(this, _combatInactiveState);
    }

    public bool HasTarget() => _targetPlayer != null;

    // ========== 사망 처리 ==========

    protected virtual void HandleDeath()
    {
        _movement?.Stop();
        Debug.Log($"{gameObject.name} 사망!");
        Destroy(gameObject, 1f);
    }
}
