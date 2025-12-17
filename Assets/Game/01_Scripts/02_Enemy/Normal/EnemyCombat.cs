using UnityEngine;

/// <summary>
/// [Role] 적 전투 시스템 - EnemyAttackDataSO 기반 공격 처리
/// CombatFSM과 연동하여 공격 사이클(선딜-공격-후딜) 관리
/// </summary>
public class EnemyCombat : MonoBehaviour
{
    [Header("공격 데이터")]
    [Tooltip("기본 공격 데이터 (필수)")]
    [SerializeField] private EnemyAttackDataSO _attackData;
    
    [Header("폴백 설정 (AttackData 없을 시 사용)")]
    [SerializeField] private float _fallbackAttackRange = 1.5f;
    [SerializeField] private float _fallbackCooldown = 1f;
    [SerializeField] private int _fallbackDamage = 10;

    // 컴포넌트 참조
    private EnemyController _controller;
    private float _lastAttackTime = -999f;
    
    // 현재 공격 상태
    private bool _isAttacking = false;
    private float _attackStartTime;

    // ========== 프로퍼티 ==========
    
    /// <summary>현재 사용 중인 공격 데이터</summary>
    public EnemyAttackDataSO AttackData => _attackData;
    
    /// <summary>공격 사거리</summary>
    public float AttackRange => _attackData != null ? _attackData.attackRange : _fallbackAttackRange;
    
    /// <summary>공격력</summary>
    public int AttackDamage => _attackData != null ? _attackData.baseDamage : _fallbackDamage;
    
    /// <summary>선딜 시간</summary>
    public float WindupDuration => _attackData?.windupDuration ?? 0.3f;
    
    /// <summary>공격 지속 시간</summary>
    public float AttackDuration => _attackData?.attackDuration ?? 0.2f;
    
    /// <summary>후딜 시간</summary>
    public float RecoveryDuration => _attackData?.recoveryDuration ?? 0.5f;
    
    /// <summary>쿨다운</summary>
    public float Cooldown => _attackData != null ? _attackData.cooldown : _fallbackCooldown;
    
    /// <summary>공격 중 이동 가능 여부</summary>
    public bool CanMoveWhileAttacking => _attackData?.canMoveWhileAttacking ?? false;
    
    /// <summary>이동 잠금 필요 여부 (공격 중 이동 불가 시)</summary>
    public bool RequiresMovementLock => !CanMoveWhileAttacking;

    // ========== 초기화 ==========
    
    private void Awake()
    {
        _controller = GetComponent<EnemyController>();
        
        // EnemyDataSO가 있으면 공격 데이터 자동 연동
        if (_controller?.EnemyData != null)
        {
            Initialize(_controller.EnemyData);
        }
    }
    
    /// <summary>
    /// EnemyDataSO 기반 초기화 - primaryAttack 연동
    /// </summary>
    public void Initialize(EnemyDataSO data)
    {
        if (data.primaryAttack != null)
        {
            _attackData = data.primaryAttack;
        }
    }

    // ========== 공격 조건 체크 ==========
    
    /// <summary>
    /// 공격 가능 여부 확인 (사거리 + 쿨다운)
    /// </summary>
    public bool CanAttack()
    {
        if (_controller == null || _controller.CurrentTarget == null) return false;

        // 사거리 체크
        float distance = Vector3.Distance(transform.position, _controller.CurrentTarget.position);
        if (distance > AttackRange) return false;

        // 쿨다운 체크
        if (Time.time < _lastAttackTime + Cooldown) return false;

        return true;
    }
    
    /// <summary>
    /// 타겟이 사거리 내에 있는지 확인
    /// </summary>
    public bool IsTargetInRange()
    {
        if (_controller == null || _controller.CurrentTarget == null) return false;
        
        float distance = Vector3.Distance(transform.position, _controller.CurrentTarget.position);
        return distance <= AttackRange;
    }
    
    /// <summary>
    /// 쿨다운 완료 여부
    /// </summary>
    public bool IsCooldownReady()
    {
        return Time.time >= _lastAttackTime + Cooldown;
    }

    // ========== 공격 실행 ==========
    
    /// <summary>
    /// 공격 시작 (CombatAttackingState에서 호출)
    /// </summary>
    public void StartAttack()
    {
        _isAttacking = true;
        _attackStartTime = Time.time;
    }
    
    /// <summary>
    /// 실제 데미지 적용 (공격 타이밍에 호출)
    /// </summary>
    public void ExecuteDamage()
    {
        if (_controller == null || _controller.CurrentTarget == null) return;

        Transform target = _controller.CurrentTarget;
        Vector3 attackDir = (target.position - transform.position).normalized;
        
        // 공격 각도 체크 (설정된 경우)
        if (_attackData != null && _attackData.attackAngle < 360f)
        {
            float angle = Vector3.Angle(transform.forward, attackDir);
            if (angle > _attackData.attackAngle / 2f) return; // 각도 밖
        }

        // IDamageable로 데미지 전달
        if (target.TryGetComponent(out IDamageable damageable))
        {
            int damage = _attackData != null ? _attackData.CalculateDamage() : _fallbackDamage;
            Vector3 knockbackDir = _attackData != null 
                ? _attackData.CalculateKnockbackDirection(attackDir) 
                : attackDir;
            
            damageable.TakeDamage(damage, target.position, knockbackDir);
            
            // VFX 생성
            if (_attackData?.attackVFXPrefab != null)
            {
                Instantiate(_attackData.attackVFXPrefab, target.position, Quaternion.identity);
            }
            
            // SFX 재생
            if (_attackData?.attackSound != null)
            {
                AudioSource.PlayClipAtPoint(_attackData.attackSound, transform.position);
            }
        }
        
        _lastAttackTime = Time.time;
    }
    
    /// <summary>
    /// 공격 종료 (CombatRecoveryState에서 호출)
    /// </summary>
    public void EndAttack()
    {
        _isAttacking = false;
    }
    
    /// <summary>
    /// 레거시 호환용 - 즉시 공격 시도
    /// </summary>
    public void TryAttack()
    {
        if (!CanAttack()) return;
        
        StartAttack();
        ExecuteDamage();
        EndAttack();
    }

#if UNITY_EDITOR
    // 공격 사거리 시각화 (에디터 전용)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
        
        // 공격 각도 시각화
        if (_attackData != null && _attackData.attackAngle < 360f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            float halfAngle = _attackData.attackAngle / 2f;
            Vector3 forward = transform.forward * AttackRange;
            
            Quaternion leftRot = Quaternion.Euler(0, -halfAngle, 0);
            Quaternion rightRot = Quaternion.Euler(0, halfAngle, 0);
            
            Gizmos.DrawLine(transform.position, transform.position + leftRot * forward);
            Gizmos.DrawLine(transform.position, transform.position + rightRot * forward);
        }
    }
#endif
}
