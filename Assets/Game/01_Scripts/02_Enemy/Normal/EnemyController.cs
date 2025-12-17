using System;
using UnityEngine;
using UnityEngine.AI;

// [역할] 적 AI 총괄 컨트롤러 - 병렬 FSM 관리 (이동 + 전투)
// 리팩토링: 단일 FSM → MovementFSM + CombatFSM 병렬 실행
// 스턴 시스템: Epic/Boss만 IStunnable 구현
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemySenses))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IStunnable
{
    [Header("적 데이터")]
    [Tooltip("EnemyDataSO를 할당 (필수)")]
    [SerializeField] private EnemyDataSO _enemyData;

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
    private StunnedMovementState _stunnedState = new StunnedMovementState(); // 스턴 상태

    // === 스턴 시스템 (Epic/Boss 전용) ===
    private bool _isStunned = false;
    private float _stunEndTime = 0f;
    public event Action OnStunStart;
    public event Action OnStunEnd;

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
    
    /// <summary>EnemyDataSO 참조</summary>
    public EnemyDataSO EnemyData => _enemyData;
    
    /// <summary>티어 (Normal, Elite, Epic, Boss)</summary>
    public EnemyTier Tier => _enemyData?.tier ?? EnemyTier.Normal;
    
    /// <summary>Zone 내 이동 제한 여부 (Epic/Boss)</summary>
    public bool RestrictToZone => Tier == EnemyTier.Epic || Tier == EnemyTier.Boss;
    
    /// <summary>순찰 동작 사용 여부 (Epic/Boss)</summary>
    public bool UsesPatrol => Tier == EnemyTier.Epic || Tier == EnemyTier.Boss;
    
    /// <summary>IsEpic은 더 이상 사용하지 않음 - RestrictToZone 또는 Tier 사용</summary>
    [System.Obsolete("RestrictToZone 또는 Tier를 사용하세요")]
    public bool IsEpic => RestrictToZone;

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
        // FSM 초기화는 Start에서 수행 (Spawner에서 BoundZones 설정 후 실행되도록)
    }
    
    private void Start()
    {
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

        // 이동 FSM 시작 상태: 티어별 분기
        if (RestrictToZone)
        {
            // Epic/Boss: Patrol (순찰하며 탐색)
            _movementFSM.ChangeState(_patrolState, this);
        }
        else
        {
            // Normal: 즉시 ChaseState (플레이어 찾아서 추격)
            // ChaseState.Execute에서 타겟이 없으면 자동으로 플레이어 검색
            _movementFSM.ChangeState(_chaseState, this);
        }
        
        // 전투 FSM 시작 상태: Inactive (타겟 없음)
        _combatFSM.ChangeState(_combatInactiveState, this);
    }

    // LateUpdate: NavMeshAgent의 Update 처리 이후 FSM 실행
    // 경로 계산과 FSM 로직의 충돌 방지
    private void LateUpdate()
    {
        // 사망 시 모든 FSM 정지
        if (_stats != null && _stats.IsDead) return;

        // === 스턴 종료 체크 ===
        CheckStunEnd();

        // === 병렬 FSM 실행 ===
        _movementFSM?.Update(this);
        _combatFSM?.Update(this);
    }

    // ========== 스턴 시스템 (IStunnable 구현) ==========

    /// <summary>현재 스턴 상태 여부</summary>
    public bool IsStunned => _isStunned;

    /// <summary>
    /// 스턴 적용 (Epic/Boss만 가능)
    /// </summary>
    public virtual void ApplyStun(float duration)
    {
        // Normal 몬스터는 스턴 불가
        if (!RestrictToZone)
        {
            Debug.LogWarning($"[Stun] {name}: Normal 몬스터는 스턴 불가");
            return;
        }

        // 이미 스턴 중이면 더 긴 시간으로 갱신
        float newEndTime = Time.time + duration;
        if (_isStunned && newEndTime <= _stunEndTime)
        {
            return; // 기존 스턴이 더 길면 무시
        }

        bool wasStunned = _isStunned;
        _isStunned = true;
        _stunEndTime = newEndTime;

        // 첫 스턴 진입 시에만 상태 전환
        if (!wasStunned)
        {
            // MovementFSM 강제 스턴 상태 전환
            _movementFSM?.ForceStunState(_stunnedState, this);
            
            // CombatFSM 리셋 (공격 중단)
            _combatFSM?.Reset(this, _combatInactiveState);

            OnStunStart?.Invoke();
            Debug.Log($"[Stun] {name}: 스턴 적용 ({duration}초)");
        }
    }

    /// <summary>스턴 즉시 해제</summary>
    public virtual void ClearStun()
    {
        if (!_isStunned) return;

        _isStunned = false;
        _stunEndTime = 0f;

        // 이전 상태로 복원
        _movementFSM?.RestoreFromStun(this);

        OnStunEnd?.Invoke();
        Debug.Log($"[Stun] {name}: 스턴 해제");
    }

    /// <summary>스턴 종료 시간 체크 (LateUpdate에서 호출)</summary>
    private void CheckStunEnd()
    {
        if (_isStunned && Time.time >= _stunEndTime)
        {
            ClearStun();
        }
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
        
        // Epic/Boss: Zone 플래그만 설정 (EnemySenses가 감지 후 타겟 설정)
        // Normal: 즉시 타겟 설정 및 추격
        if (!RestrictToZone)
        {
            SetTarget(player);
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
        if (_targetPlayer != target)
             Debug.Log($"[Controller] {name}: 타겟 설정 -> {target.name}");
        _targetPlayer = target;
    }

    public void ClearTarget()
    {
        if (_targetPlayer != null)
             Debug.LogWarning($"[Controller] {name}: 타겟 해제됨!");
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
