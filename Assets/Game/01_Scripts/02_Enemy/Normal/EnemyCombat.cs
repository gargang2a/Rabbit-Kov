using UnityEngine;

// [역할] 적 전투 시스템 - EnemyAttackDataSO 기반 공격 처리
public class EnemyCombat : MonoBehaviour
{
    [Header("공격 데이터")]
    [Tooltip("기본 공격 데이터 (필수)")]
    [SerializeField] private EnemyAttackDataSO _attackData; // 공격 데이터 SO
    
    [Header("폴백 설정 (AttackData 없을 시)")]
    [SerializeField] private float _fallbackAttackRange = 1.5f; // 기본 사거리
    [SerializeField] private float _fallbackCooldown = 1f;      // 기본 쿨다운
    [SerializeField] private int _fallbackDamage = 10;          // 기본 데미지

    private EnemyController _controller;     // 컨트롤러 참조
    private float _lastAttackTime = -999f;   // 마지막 공격 시간
    
    private bool _isAttacking = false;       // 공격 중 여부
    private float _attackStartTime;          // 공격 시작 시간

    // 프로퍼티
    public EnemyAttackDataSO AttackData => _attackData;
    
    // 사거리
    public float AttackRange
    {
        get
        {
            if (_attackData != null)
            {
                return _attackData.attackRange;
            }
            return _fallbackAttackRange;
        }
    }
    
    // 데미지
    public int AttackDamage
    {
        get
        {
            if (_attackData != null)
            {
                return _attackData.baseDamage;
            }
            return _fallbackDamage;
        }
    }
    
    // 선딜 시간
    public float WindupDuration
    {
        get
        {
            if (_attackData != null)
            {
                return _attackData.windupDuration;
            }
            return 0.3f;
        }
    }
    
    // 공격 시간
    public float AttackDuration
    {
        get
        {
            if (_attackData != null)
            {
                return _attackData.attackDuration;
            }
            return 0.2f;
        }
    }
    
    // 후딜 시간
    public float RecoveryDuration
    {
        get
        {
            if (_attackData != null)
            {
                return _attackData.recoveryDuration;
            }
            return 0.5f;
        }
    }
    
    // 쿨다운
    public float Cooldown
    {
        get
        {
            if (_attackData != null)
            {
                return _attackData.cooldown;
            }
            return _fallbackCooldown;
        }
    }
    
    // 이동 공격 가능 여부
    public bool CanMoveWhileAttacking
    {
        get
        {
            if (_attackData != null)
            {
                return _attackData.canMoveWhileAttacking;
            }
            return false;
        }
    }
    
    // 이동 잠금 필요 여부
    public bool RequiresMovementLock => !CanMoveWhileAttacking;

    private void Awake()
    {
        _controller = GetComponent<EnemyController>(); // 컨트롤러 캐싱
        
        if (_controller?.EnemyData != null) // DataSO가 있으면
        {
            Initialize(_controller.EnemyData); // 초기화
        }
    }
    
    // EnemyDataSO 기반 초기화
    public void Initialize(EnemyDataSO data)
    {
        // _attackData는 Inspector에서 직접 할당
    }

    // 공격 가능 여부 (사거리 + 쿨다운)
    public bool CanAttack()
    {
        if (_controller == null || _controller.CurrentTarget == null) return false; // 타겟 없으면 불가

        float distance = Vector3.Distance(transform.position, _controller.CurrentTarget.position); // 거리 계산
        if (distance > AttackRange) return false; // 사거리 밖이면 불가
        if (Time.time < _lastAttackTime + Cooldown) return false; // 쿨다운 중이면 불가

        return true; // 공격 가능
    }
    
    // 타겟이 사거리 내에 있는지
    public bool IsTargetInRange()
    {
        if (_controller == null || _controller.CurrentTarget == null) return false; // 타겟 없으면 불가
        
        float distance = Vector3.Distance(transform.position, _controller.CurrentTarget.position); // 거리 계산
        return distance <= AttackRange; // 사거리 내 여부
    }
    
    // 쿨다운 완료 여부
    public bool IsCooldownReady()
    {
        return Time.time >= _lastAttackTime + Cooldown; // 쿨다운 끝났는지
    }

    // 공격 시작
    public void StartAttack()
    {
        _isAttacking = true;           // 공격 중 플래그
        _attackStartTime = Time.time;  // 시작 시간 기록
    }
    
    // 데미지 적용
    public void ExecuteDamage()
    {
        if (_controller == null || _controller.CurrentTarget == null) return; // 타겟 없으면 종료

        Transform target = _controller.CurrentTarget;                           // 타겟 가져오기
        Vector3 attackDir = (target.position - transform.position).normalized;  // 공격 방향 계산
        
        // 공격 각도 체크
        if (_attackData != null && _attackData.attackAngle < 360f) // 각도 제한이 있으면
        {
            float angle = Vector3.Angle(transform.forward, attackDir); // 각도 계산
            if (angle > _attackData.attackAngle / 2f) return;           // 각도 밖이면 종료
        }

        if (target.TryGetComponent(out IDamageable damageable)) // 데미지 가능 대상이면
        {
            // 데미지 계산
            int damage;
            if (_attackData != null)
            {
                damage = _attackData.CalculateDamage();
            }
            else
            {
                damage = _fallbackDamage;
            }
            
            // 넉백 방향 계산
            Vector3 knockbackDir;
            if (_attackData != null)
            {
                knockbackDir = _attackData.CalculateKnockbackDirection(attackDir);
            }
            else
            {
                knockbackDir = attackDir;
            }
            
            damageable.TakeDamage(damage, target.position, knockbackDir); // 데미지 적용
            
            // VFX
            if (_attackData?.attackVFXPrefab != null) // VFX가 있으면
            {
                Instantiate(_attackData.attackVFXPrefab, target.position, Quaternion.identity); // VFX 생성
            }
            
            // SFX
            if (_attackData?.attackSound != null) // 사운드가 있으면
            {
                AudioSource.PlayClipAtPoint(_attackData.attackSound, transform.position); // 사운드 재생
            }
        }
        
        _lastAttackTime = Time.time; // 마지막 공격 시간 갱신
    }
    
    // 공격 종료
    public void EndAttack()
    {
        _isAttacking = false; // 공격 중 플래그 해제
    }
    
    // 레거시 호환용 - 즉시 공격
    public void TryAttack()
    {
        if (!CanAttack()) return; // 공격 불가면 종료
        
        StartAttack();    // 시작
        ExecuteDamage();  // 데미지
        EndAttack();      // 종료
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 공격 사거리
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange); // 빨간 원
        
        // 공격 각도
        if (_attackData != null && _attackData.attackAngle < 360f) // 각도 제한이 있으면
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            float halfAngle = _attackData.attackAngle / 2f;       // 절반 각도
            Vector3 forward = transform.forward * AttackRange;    // 전방 벡터
            
            Quaternion leftRot = Quaternion.Euler(0, -halfAngle, 0);  // 왼쪽 회전
            Quaternion rightRot = Quaternion.Euler(0, halfAngle, 0);  // 오른쪽 회전
            
            Gizmos.DrawLine(transform.position, transform.position + leftRot * forward);  // 왼쪽 선
            Gizmos.DrawLine(transform.position, transform.position + rightRot * forward); // 오른쪽 선
        }
    }
#endif
}
