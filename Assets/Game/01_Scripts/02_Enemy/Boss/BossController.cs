using System;
using UnityEngine;

// [역할] 보스 컨트롤러 - EnemyController 상속, 페이즈 시스템 + 보스 공격 패턴 관리
[RequireComponent(typeof(BossPhaseManager))]
public class BossController : EnemyController
{
    // 컴포넌트
    private BossPhaseManager _phaseManager;   // 페이즈 관리자
    private IBossAttack _currentAttack;       // 현재 공격
    
    // 상태
    private bool _isBossFight = false;        // 보스전 진행 중
    private float _attackCooldownTimer = 0f;  // 공격 쿨다운
    
    // 프로퍼티
    public BossPhaseManager PhaseManager => _phaseManager;
    public int CurrentPhase
    {
        get
        {
            if (_phaseManager != null)
            {
                return _phaseManager.CurrentPhase;
            }
            return 1;
        }
    }
    public bool IsBossFight => _isBossFight;

    [Header("취약점 설정")]
    [Tooltip("취약 상태 데미지 배율")]
    [SerializeField] private float _vulnerabilityMultiplier = 1.5f;
    
    [Tooltip("기본 취약 지속 시간")]
    [SerializeField] private float _defaultVulnerabilityDuration = 3f;
    
    private bool _isVulnerable = false;       // 취약 상태
    private float _vulnerabilityEndTime = 0f; // 취약 종료 시간
    
    public event Action OnVulnerableStart;    // 취약 시작 이벤트
    public event Action OnVulnerableEnd;      // 취약 종료 이벤트
    
    public bool IsVulnerable => _isVulnerable;
    
    // 데미지 배율
    public float DamageMultiplier
    {
        get
        {
            if (_isVulnerable)
            {
                return _vulnerabilityMultiplier;
            }
            return 1f;
        }
    }

    // 컴포넌트 캐싱
    protected override void CacheComponents()
    {
        base.CacheComponents();
        _phaseManager = GetComponent<BossPhaseManager>();
    }

    protected override void Start()
    {
        base.Start(); // 부모 FSM 초기화
        InitializeBoss();
    }

    // 보스 초기화
    private void InitializeBoss()
    {
        if (EnemyData == null) // 데이터 없으면
        {
            Debug.LogError($"[Boss] {name}: EnemyDataSO가 설정되지 않았습니다!");
            return;
        }
        
        if (EnemyData.tier != EnemyTier.Boss) // 티어 체크
        {
            Debug.LogWarning($"[Boss] {name}: tier가 Boss가 아닙니다! (현재: {EnemyData.tier})");
        }

        _phaseManager?.Initialize(this, EnemyData); // 페이즈 매니저 초기화
        
        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseChanged += OnPhaseChanged;
        }

        Debug.Log($"[Boss] {EnemyData.enemyName} 초기화 완료 (체력: {EnemyData.FinalMaxHealth})");
    }

    // 보스전 시작
    public void StartBossFight(Transform player)
    {
        Debug.Log($"[Boss] {name}: StartBossFight 진입!");
        if (_isBossFight) return; // 이미 진행 중
        
        _isBossFight = true;
        SetTarget(player);
        
        // 초기 쿨다운 설정 (첫 공격까지 대기 시간)
        EnemyAttackDataSO attackData = _phaseManager?.GetCurrentAttackData();
        if (attackData != null)
        {
            _attackCooldownTimer = attackData.cooldown;
        }
        
        Debug.Log($"[Boss] {name}: ChaseState로 전환");
        ChangeMovementState(ChaseMovementState);
        
        Debug.Log($"[Boss] {EnemyData?.enemyName}: 보스전 시작! (초기 쿨다운: {_attackCooldownTimer}초)");
        
        // TODO: 보스 등장 연출, UI 표시
    }

    // 보스전 종료
    public void EndBossFight()
    {
        _isBossFight = false;
        _currentAttack?.Cancel();
        
        Debug.Log($"[Boss] {EnemyData?.enemyName}: 보스전 종료!");
        
        // TODO: 보스 사망 연출, 보상 지급
    }

    // 페이즈 변경 시
    private void OnPhaseChanged(int newPhase)
    {
        _currentAttack?.Cancel(); // 현재 공격 중단
        _currentAttack = null;
        
        if (_isVulnerable) // 취약 상태 해제
        {
            ExitVulnerableState();
        }
        
        Debug.Log($"[Boss] 페이즈 {newPhase} 진입!");
        
        // TODO: 페이즈 전환 연출
    }

    // 피격 시 호출
    public void OnDamageTaken(int currentHealth, int maxHealth)
    {
        float healthRatio = (float)currentHealth / maxHealth;
        _phaseManager?.CheckPhaseTransition(healthRatio);
        
        if (currentHealth <= 0) // 사망
        {
            EndBossFight();
        }
    }

    private void Update()
    {
        if (!_isBossFight || CurrentTarget == null) return;
        if (IsStunned) return; // 스턴 중 공격 불가
        
        // 플레이어 사망 체크
        Player player = CurrentTarget.GetComponent<Player>();
        if (player != null && player.IsDead)
        {
            EndBossFight();
            ClearTarget();
            return;
        }
        
        CheckVulnerabilityEnd(); // 취약 종료 체크
        
        if (_attackCooldownTimer > 0) // 쿨다운
        {
            _attackCooldownTimer -= Time.deltaTime;
            return;
        }

        if (_currentAttack != null && _currentAttack.IsExecuting) return; // 공격 중

        TryExecuteAttack(); // 새 공격
    }

    // 공격 시도
    private void TryExecuteAttack()
    {
        GameObject attackPrefab = _phaseManager?.GetCurrentAttackPrefab();
        if (attackPrefab == null) return;

        // 프리팹을 인스턴스화 (보스 위치에 생성)
        GameObject attackInstance = Instantiate(attackPrefab, transform.position, Quaternion.identity);
        attackInstance.transform.SetParent(transform); // 보스 자식으로

        IBossAttack attack = attackInstance.GetComponent<IBossAttack>();
        if (attack == null)
        {
            Debug.LogWarning($"[Boss] 공격 프리팹에 IBossAttack 없음: {attackPrefab.name}");
            Destroy(attackInstance);
            return;
        }
        
        // 공격 데이터로 초기화
        EnemyAttackDataSO attackData = _phaseManager?.GetCurrentAttackData();
        attack.Initialize(attackData);

        _currentAttack = attack;
        _currentAttack.Execute(this, CurrentTarget);
        
        // 쿨다운 = 전체 공격 사이클 (Windup + Attack + Recovery + Cooldown)
        if (attackData != null)
        {
            _attackCooldownTimer = attackData.TotalCycleDuration + attackData.cooldown;
        }
        else
        {
            _attackCooldownTimer = attack.Cooldown;
        }
        
        // 공격 완료 후 인스턴스 삭제
        float destroyDelay = attackData != null ? attackData.TotalCycleDuration + 2f : attack.Cooldown + 5f;
        Destroy(attackInstance, destroyDelay);
        
        Debug.Log($"[Boss] 공격 실행: {attack.AttackName} (다음 공격까지: {_attackCooldownTimer}초)");
    }

    private void OnDestroy()
    {
        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseChanged -= OnPhaseChanged;
        }
    }
    
    // 취약 상태 진입
    public void EnterVulnerableState(float duration = -1f)
    {
        if (duration < 0)
        {
            duration = _defaultVulnerabilityDuration;
        }
        
        _isVulnerable = true;
        _vulnerabilityEndTime = Time.time + duration;
        
        OnVulnerableStart?.Invoke();
        Debug.Log($"[Boss] {name}: 취약 상태 진입! ({duration}초, 데미지 {_vulnerabilityMultiplier}배)");
    }
    
    // 취약 상태 해제
    public void ExitVulnerableState()
    {
        if (!_isVulnerable) return;
        
        _isVulnerable = false;
        _vulnerabilityEndTime = 0f;
        
        OnVulnerableEnd?.Invoke();
        Debug.Log($"[Boss] {name}: 취약 상태 종료");
    }
    
    // 취약 종료 시간 체크
    private void CheckVulnerabilityEnd()
    {
        if (_isVulnerable && Time.time >= _vulnerabilityEndTime)
        {
            ExitVulnerableState();
        }
    }
    
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
