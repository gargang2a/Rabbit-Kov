using UnityEngine;

/// <summary>
/// [Role] 보스 컨트롤러 - EnemyController 상속
/// 페이즈 시스템 및 보스 전용 공격 패턴 관리
/// </summary>
[RequireComponent(typeof(BossPhaseManager))]
public class BossController : EnemyController
{
    [Header("보스 설정")]
    [SerializeField] private BossDataSO _bossData;
    
    // 컴포넌트
    private BossPhaseManager _phaseManager;
    private IBossAttack _currentAttack;
    
    // 상태
    private bool _isBossFight = false;
    private float _attackCooldownTimer = 0f;
    
    // 프로퍼티
    public BossDataSO BossData => _bossData;
    public BossPhaseManager PhaseManager => _phaseManager;
    public int CurrentPhase => _phaseManager?.CurrentPhase ?? 1;
    public bool IsBossFight => _isBossFight;

    // ========== 초기화 ==========

    protected override void CacheComponents()
    {
        base.CacheComponents();
        _phaseManager = GetComponent<BossPhaseManager>();
    }

    private void Start()
    {
        InitializeBoss();
    }

    private void InitializeBoss()
    {
        if (_bossData == null)
        {
            Debug.LogError($"[Boss] {name}: BossDataSO가 설정되지 않았습니다!");
            return;
        }

        // 페이즈 매니저 초기화
        _phaseManager?.Initialize(this, _bossData);
        
        // 페이즈 전환 이벤트 구독
        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseChanged += OnPhaseChanged;
        }

        // 스탯 설정 (EnemyStats가 있으면 오버라이드)
        if (Stats != null)
        {
            Stats.SetMaxHealth(_bossData.maxHealth);
        }

        Debug.Log($"[Boss] {_bossData.bossName} 초기화 완료 (체력: {_bossData.maxHealth})");
    }

    // ========== 보스전 시작/종료 ==========

    /// <summary>
    /// 보스전 시작 (플레이어가 보스 Zone 진입 시 호출)
    /// </summary>
    public void StartBossFight(Transform player)
    {
        if (_isBossFight) return;
        
        _isBossFight = true;
        SetTarget(player);
        
        Debug.Log($"[Boss] {_bossData?.bossName ?? name}: 보스전 시작!");
        
        // TODO: 보스 등장 연출, UI 표시 등
    }

    /// <summary>
    /// 보스전 종료 (보스 사망 시)
    /// </summary>
    public void EndBossFight()
    {
        _isBossFight = false;
        _currentAttack?.Cancel();
        
        Debug.Log($"[Boss] {_bossData?.bossName ?? name}: 보스전 종료!");
        
        // TODO: 보스 사망 연출, 보상 지급 등
    }

    // ========== 페이즈 시스템 ==========

    private void OnPhaseChanged(int newPhase)
    {
        // 현재 공격 중단
        _currentAttack?.Cancel();
        _currentAttack = null;
        
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
}
