using System;
using UnityEngine;
using UnityEngine.AI;

// [역할] 적 AI 총괄 컨트롤러 - 병렬 FSM 관리 (이동 + 전투)
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemySenses))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IStunnable
{
    [Header("적 데이터")]
    [Tooltip("EnemyDataSO를 할당 (필수)")]
    [SerializeField] private EnemyDataSO _enemyData; // 적 데이터 SO

    // 컴포넌트 참조
    private EnemyStats _stats;       // 체력 관리
    private EnemyMovement _movement; // 이동 관리
    private EnemySenses _senses;     // 감지 시스템
    private EnemyCombat _combat;     // 전투 시스템
    private Transform _targetPlayer; // 현재 타겟

    // 병렬 FSM
    private MovementFSM _movementFSM; // 이동 상태 머신
    private CombatFSM _combatFSM;     // 전투 상태 머신

    // 이동 상태 객체 (GC 방지용 재사용)
    private PatrolState _patrolState = new PatrolState();           // 순찰
    private ChaseState _chaseState = new ChaseState();              // 추격
    private StoppedState _stoppedState = new StoppedState();        // 정지
    private ReturnState _returnState = new ReturnState();           // 귀환
    private WaitState _waitState = new WaitState();                 // 대기 (Normal 전용)
    private StunnedMovementState _stunnedState = new StunnedMovementState(); // 스턴

    // 스턴 시스템 (Epic/Boss 전용)
    private bool _isStunned = false;   // 스턴 여부
    private float _stunEndTime = 0f;   // 스턴 종료 시간
    public event Action OnStunStart;   // 스턴 시작 이벤트
    public event Action OnStunEnd;     // 스턴 종료 이벤트

    // 전투 상태 객체
    private CombatInactiveState _combatInactiveState = new CombatInactiveState();   // 비활성
    private CombatReadyState _combatReadyState = new CombatReadyState();            // 준비
    private CombatWindupState _combatWindupState = new CombatWindupState();         // 선딜
    private CombatAttackingState _combatAttackingState = new CombatAttackingState();// 공격 중
    private CombatRecoveryState _combatRecoveryState = new CombatRecoveryState();   // 후딜

    // Zone 관련
    private Collider[] _boundZones;       // 이동 제한 Zone
    private bool _isPlayerInZone = false; // 플레이어 Zone 진입 여부

    // 프로퍼티 (컴포넌트)
    public EnemyStats Stats => _stats;
    public EnemyMovement Movement => _movement;
    public EnemySenses Senses => _senses;
    public EnemyCombat Combat => _combat;
    public Transform CurrentTarget => _targetPlayer;
    public Collider[] BoundZones => _boundZones;
    public bool IsPlayerInZone => _isPlayerInZone;
    public EnemyDataSO EnemyData => _enemyData;
    public EnemyTier Tier => _enemyData?.tier ?? EnemyTier.Normal;                  // 티어
    public bool RestrictToZone => Tier == EnemyTier.Epic || Tier == EnemyTier.Boss; // Zone 제한
    public bool UsesPatrol => Tier == EnemyTier.Epic || Tier == EnemyTier.Boss;     // 순찰 사용

    [System.Obsolete("RestrictToZone 또는 Tier를 사용하세요")]
    public bool IsEpic => RestrictToZone;

    // 프로퍼티 (이동 상태)
    public IMovementState PatrolMovementState => _patrolState;
    public IMovementState ChaseMovementState => _chaseState;
    public IMovementState StoppedMovementState => _stoppedState;
    public IMovementState ReturnMovementState => _returnState;
    public IMovementState WaitMovementState => _waitState;

    // 프로퍼티 (전투 상태)
    public ICombatState CombatInactiveState => _combatInactiveState;
    public ICombatState CombatReadyState => _combatReadyState;
    public ICombatState CombatWindupState => _combatWindupState;
    public ICombatState CombatAttackingState => _combatAttackingState;
    public ICombatState CombatRecoveryState => _combatRecoveryState;

    // 디버깅용 현재 상태 이름
    public string CurrentMovementStateName => _movementFSM?.CurrentStateName ?? "None";
    public string CurrentCombatStateName => _combatFSM?.CurrentStateName ?? "None";

    private void Awake()
    {
        CacheComponents(); // 컴포넌트 캐싱
    }
    
    protected virtual void Start()
    {
        InitializeFSMs(); // FSM 초기화
    }

    // 컴포넌트 캐싱 및 이벤트 연결
    protected virtual void CacheComponents()
    {
        _stats = GetComponent<EnemyStats>();       // 체력
        _movement = GetComponent<EnemyMovement>(); // 이동
        _senses = GetComponent<EnemySenses>();     // 감지
        _combat = GetComponent<EnemyCombat>();     // 전투

        if (_stats != null) // 체력 컴포넌트가 있으면
        {
            _stats.OnDeath += HandleDeath; // 사망 이벤트 연결
        }
    }

    // FSM 초기화 및 시작 상태 설정
    private void InitializeFSMs()
    {
        _movementFSM = new MovementFSM(); // 이동 FSM 생성
        _combatFSM = new CombatFSM();     // 전투 FSM 생성

        if (RestrictToZone) // Epic/Boss면
        {
            _movementFSM.ChangeState(_patrolState, this); // 순찰로 시작
        }
        else // Normal이면
        {
            _movementFSM.ChangeState(_chaseState, this);  // 즉시 추격
        }
        
        _combatFSM.ChangeState(_combatInactiveState, this); // 전투: 비활성으로 시작
    }

    // 매 프레임 FSM 실행 (NavMeshAgent 처리 후)
    private void LateUpdate()
    {
        if (_stats != null && _stats.IsDead) return; // 죽었으면 무시

        CheckStunEnd(); // 스턴 종료 체크

        _movementFSM?.Update(this); // 이동 FSM 실행
        _combatFSM?.Update(this);   // 전투 FSM 실행
    }

    // 스턴 상태 프로퍼티
    public bool IsStunned => _isStunned;

    // 스턴 적용 (Epic/Boss만)
    public virtual void ApplyStun(float duration)
    {
        if (!RestrictToZone) // Normal은 스턴 불가
        {
            Debug.LogWarning($"[Stun] {name}: Normal 몬스터는 스턴 불가");
            return;
        }

        float newEndTime = Time.time + duration;             // 새 종료 시간 계산
        if (_isStunned && newEndTime <= _stunEndTime) return; // 기존 스턴이 더 길면 무시

        bool wasStunned = _isStunned; // 이전 스턴 상태 저장
        _isStunned = true;            // 스턴 설정
        _stunEndTime = newEndTime;    // 종료 시간 설정

        if (!wasStunned) // 첫 스턴 진입시에만
        {
            _movementFSM?.ForceStunState(_stunnedState, this); // 이동 FSM 스턴 상태로
            _combatFSM?.Reset(this, _combatInactiveState);     // 전투 FSM 리셋
            OnStunStart?.Invoke();                              // 스턴 시작 이벤트
            Debug.Log($"[Stun] {name}: 스턴 적용 ({duration}초)");
        }
    }

    // 스턴 즉시 해제
    public virtual void ClearStun()
    {
        if (!_isStunned) return; // 스턴 아니면 무시

        _isStunned = false;              // 스턴 해제
        _stunEndTime = 0f;               // 종료 시간 초기화
        _movementFSM?.RestoreFromStun(this); // 이전 상태 복원
        OnStunEnd?.Invoke();             // 스턴 종료 이벤트
        Debug.Log($"[Stun] {name}: 스턴 해제");
    }

    // 스턴 종료 시간 체크
    private void CheckStunEnd()
    {
        if (_isStunned && Time.time >= _stunEndTime) // 스턴 중이고 시간 지났으면
        {
            ClearStun(); // 스턴 해제
        }
    }

    private void OnDestroy()
    {
        if (_stats != null) // 정리
        {
            _stats.OnDeath -= HandleDeath; // 이벤트 해제
        }
    }

    // Zone 설정 (복수)
    public void SetBoundZones(Collider[] zones)
    {
        _boundZones = zones;              // Zone 저장
        _movement?.SetBoundZones(zones);  // Movement에도 전달
    }

    // Zone 설정 (단일)
    public void SetBoundZone(Collider zone)
    {
        if (zone != null)
        {
            _boundZones = new Collider[] { zone }; // Zone 배열 생성
        }
        else
        {
            _boundZones = null; // Zone 없음
        }
        _movement?.SetBoundZones(_boundZones);                        // Movement에 전달
    }

    // 플레이어 Zone 진입
    public void OnPlayerEnterZone(Transform player)
    {
        _isPlayerInZone = true; // 플래그 설정
        
        if (!RestrictToZone) // Normal이면
        {
            SetTarget(player);               // 타겟 설정
            ChangeMovementState(_chaseState); // 추격 시작
        }
    }

    // 플레이어 Zone 이탈
    public void OnPlayerExitZone()
    {
        _isPlayerInZone = false; // 플래그 해제
    }

    // 이동 상태 전환
    public void ChangeMovementState(IMovementState newState)
    {
        _movementFSM?.ChangeState(newState, this); // FSM에 전달
    }

    // 전투 상태 전환
    public void ChangeCombatState(ICombatState newState)
    {
        _combatFSM?.ChangeState(newState, this); // FSM에 전달
    }

    // 이동 잠금 (정지 공격 시)
    public void LockMovement()
    {
        _movementFSM?.Lock(this, _stoppedState); // 정지 상태로 잠금
    }

    // 이동 잠금 해제
    public void UnlockMovement()
    {
        _movementFSM?.Unlock(this); // 잠금 해제
    }

    // 이동 잠금 여부
    public bool IsMovementLocked => _movementFSM?.IsLocked ?? false;

    // 타겟 설정
    public void SetTarget(Transform target)
    {
        if (_targetPlayer != target) // 타겟이 바뀌면
             Debug.Log($"[Controller] {name}: 타겟 설정 -> {target.name}");
        _targetPlayer = target; // 타겟 저장
    }

    // 타겟 해제
    public void ClearTarget()
    {
        if (_targetPlayer != null) // 타겟이 있었으면
             Debug.LogWarning($"[Controller] {name}: 타겟 해제됨!");
        _targetPlayer = null;                          // 타겟 해제
        _combatFSM?.Reset(this, _combatInactiveState); // 전투 FSM 리셋
    }

    // 타겟 유무
    public bool HasTarget() => _targetPlayer != null;

    // 사망 처리
    protected virtual void HandleDeath()
    {
        _movement?.Stop();                   // 이동 정지
        Debug.Log($"{gameObject.name} 사망!");
        Destroy(gameObject, 1f);             // 1초 후 파괴
    }
}
