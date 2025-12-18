using System;
using UnityEngine;

/// <summary>
/// [Role] 보스 컨트롤러 - EnemyController 상속
/// 페이즈 시스템 및 보스 전용 공격 패턴 관리
/// EnemyDataSO의 tier=Boss 필드 사용
/// </summary>
[RequireComponent(typeof(BossPhaseManager))]
public class BossController : EnemyController
{
    // 컴포넌트
    private BossPhaseManager _phaseManager;
    private IBossAttack _currentAttack;
    
    // 상태
    private bool _isBossFight = false;
    private float _attackCooldownTimer = 0f;
    
    // 프로퍼티 (EnemyData의 보스 필드 접근)
    public BossPhaseManager PhaseManager => _phaseManager;
    public int CurrentPhase => _phaseManager?.CurrentPhase ?? 1;
    public bool IsBossFight => _isBossFight;

    // ========== 취약점 시스템 ==========
    
    [Header("취약점 설정")]
    [Tooltip("취약 상태 데미지 배율")]
    [SerializeField] private float _vulnerabilityMultiplier = 1.5f;
    
    [Tooltip("기본 취약 지속 시간")]
    [SerializeField] private float _defaultVulnerabilityDuration = 3f;
    
    private bool _isVulnerable = false;
    private float _vulnerabilityEndTime = 0f;
    
    /// <summary>취약 상태 시작 이벤트</summary>
    public event Action OnVulnerableStart;
    
    /// <summary>취약 상태 종료 이벤트</summary>
    public event Action OnVulnerableEnd;
    
    /// <summary>현재 취약 상태 여부</summary>
    public bool IsVulnerable => _isVulnerable;
    
    /// <summary>데미지 배율 (취약 시 1.5배, 아니면 1배)</summary>
    public float DamageMultiplier => _isVulnerable ? _vulnerabilityMultiplier : 1f;

    // ========== 초기화 ==========

    protected override void CacheComponents()
    {
        base.CacheComponents();
        _phaseManager = GetComponent<BossPhaseManager>();
    }

    protected override void Start()
    {
        base.Start(); // 부모의 FSM 초기화 실행
        InitializeBoss();
    }

    private void InitializeBoss()
    {
        // EnemyDataSO 체크 (부모 클래스의 EnemyData 사용)
        if (EnemyData == null)
        {
            Debug.LogError($"[Boss] {name}: EnemyDataSO가 설정되지 않았습니다!");
            return;
        }
        
        // tier 체크
        if (EnemyData.tier != EnemyTier.Boss)
        {
            Debug.LogWarning($"[Boss] {name}: EnemyDataSO의 tier가 Boss가 아닙니다! (현재: {EnemyData.tier})");
        }

        // 페이즈 매니저 초기화 (EnemyDataSO 전달)
        _phaseManager?.Initialize(this, EnemyData);
        
        // 페이즈 전환 이벤트 구독
        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseChanged += OnPhaseChanged;
        }

        Debug.Log($"[Boss] {EnemyData.enemyName} 초기화 완료 (체력: {EnemyData.FinalMaxHealth})");
    }

    // ========== 보스전 시작/종료 ==========

    /// <summary>
    /// 보스전 시작 (플레이어가 보스 Zone 진입 시 호출)
    /// </summary>
    public void StartBossFight(Transform player)
    {
        Debug.Log($"[Boss] {name}: StartBossFight 진입! _isBossFight={_isBossFight}");
        if (_isBossFight) 
        {
            Debug.Log($"[Boss] {name}: 이미 보스전 중이므로 리턴");
            return;
        }
        
        _isBossFight = true;
        SetTarget(player);
        
        // 이동 상태를 ChaseState로 전환 (보스는 RestrictToZone=true라서 직접 전환)
        Debug.Log($"[Boss] {name}: ChangeMovementState(ChaseMovementState) 호출 예정, State={ChaseMovementState}");
        ChangeMovementState(ChaseMovementState);
        Debug.Log($"[Boss] {name}: ChangeMovementState 호출 완료");
        
        Debug.Log($"[Boss] {EnemyData?.enemyName ?? name}: 보스전 시작!");
        
        // TODO: 보스 등장 연출, UI 표시 등
    }

    /// <summary>
    /// 보스전 종료 (보스 사망 시)
    /// </summary>
    public void EndBossFight()
    {
        _isBossFight = false;
        _currentAttack?.Cancel();
        
        Debug.Log($"[Boss] {EnemyData?.enemyName ?? name}: 보스전 종료!");
        
        // TODO: 보스 사망 연출, 보상 지급 등
    }

    // ========== 페이즈 시스템 ==========

    private void OnPhaseChanged(int newPhase)
    {
        // 현재 공격 중단
        _currentAttack?.Cancel();
        _currentAttack = null;
        
        // 취약 상태 해제 (페이즈 전환 시)
        if (_isVulnerable)
        {
            ExitVulnerableState();
        }
        
        // 페이즈 전환 연출
        Debug.Log($"[Boss] 페이즈 {newPhase} 진입! 새로운 공격 패턴 활성화");
        
        // TODO: 페이즈 전환 연출 (잠시 무적, 이펙트 등)
    }

    // ========== 체력 연동 ==========

    /// <summary>
    /// 피격 시 호출 (EnemyStats에서 호출)
    /// </summary>
    public void OnDamageTaken(int currentHealth, int maxHealth)
    {
        float healthRatio = (float)currentHealth / maxHealth;
        _phaseManager?.CheckPhaseTransition(healthRatio);
        
        // 사망 체크
        if (currentHealth <= 0)
        {
            EndBossFight();
        }
    }

    // ========== 공격 시스템 ==========

    private void Update()
    {
        if (!_isBossFight || CurrentTarget == null) return;
        
        // 스턴 중이면 공격 불가
        if (IsStunned) return;
        
        // 취약 종료 체크
        CheckVulnerabilityEnd();
        
        // 공격 쿨다운
        if (_attackCooldownTimer > 0)
        {
            _attackCooldownTimer -= Time.deltaTime;
            return;
        }

        // 공격 실행 중이면 대기
        if (_currentAttack != null && _currentAttack.IsExecuting) return;

        // 새 공격 시작
        TryExecuteAttack();
    }

    private void TryExecuteAttack()
    {
        // 현재 페이즈의 공격 프리팹 가져오기
        GameObject attackPrefab = _phaseManager?.GetCurrentAttackPrefab();
        if (attackPrefab == null) return;

        // 공격 컴포넌트 가져오기
        IBossAttack attack = attackPrefab.GetComponent<IBossAttack>();
        if (attack == null)
        {
            Debug.LogWarning($"[Boss] 공격 프리팹에 IBossAttack 컴포넌트가 없습니다: {attackPrefab.name}");
            return;
        }

        // 공격 실행
        _currentAttack = attack;
        _currentAttack.Execute(this, CurrentTarget);
        _attackCooldownTimer = attack.Cooldown;
        
        Debug.Log($"[Boss] 공격 실행: {attack.AttackName}");
    }

    // ========== 정리 ==========

    private void OnDestroy()
    {
        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseChanged -= OnPhaseChanged;
        }
    }
    
    // ========== 취약점 시스템 메서드 ==========
    
    /// <summary>
    /// 취약 상태 진입 (공격 패턴 후 호출)
    /// </summary>
    /// <param name="duration">취약 지속 시간 (-1이면 기본값 사용)</param>
    public void EnterVulnerableState(float duration = -1f)
    {
        if (duration < 0) duration = _defaultVulnerabilityDuration;
        
        _isVulnerable = true;
        _vulnerabilityEndTime = Time.time + duration;
        
        OnVulnerableStart?.Invoke();
        Debug.Log($"[Boss] {name}: 취약 상태 진입! ({duration}초, 데미지 {_vulnerabilityMultiplier}배)");
    }
    
    /// <summary>
    /// 취약 상태 해제
    /// </summary>
    public void ExitVulnerableState()
    {
        if (!_isVulnerable) return;
        
        _isVulnerable = false;
        _vulnerabilityEndTime = 0f;
        
        OnVulnerableEnd?.Invoke();
        Debug.Log($"[Boss] {name}: 취약 상태 종료");
    }
    
    /// <summary>취약 종료 시간 체크</summary>
    private void CheckVulnerabilityEnd()
    {
        if (_isVulnerable && Time.time >= _vulnerabilityEndTime)
        {
            ExitVulnerableState();
        }
    }
    
    // ========== 테스트 메서드 (Editor Only) ==========
    
#if UNITY_EDITOR
    [ContextMenu("Test Stun (3s)")]
    private void TestStun()
    {
        ApplyStun(3f);
    }
    
    [ContextMenu("Test Vulnerability (3s)")]
    private void TestVulnerability()
    {
        EnterVulnerableState(3f);
    }
#endif
}
