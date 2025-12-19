using System;
using UnityEngine;

// [역할] 적 체력 시스템 - HP, 데미지 처리, 사망 이벤트 (IDamageable 구현)
public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Fallback Settings (DataSO Overrides)")]
    [SerializeField] private int _maxHealth = 100;   // 최대 체력
    private int _currentHealth;                       // 현재 체력

    [Header("Combat Feedback")]
    [Tooltip("피격 시 넉백 강도")]
    [SerializeField] private float _knockbackForce = 3f; // 넉백 강도
    
    private Rigidbody _rb; // 물리 컴포넌트

    // 이벤트
    public event Action OnHealthChanged;  // 체력 변동 시
    public event Action OnDeath;          // 사망 시
    public event Action<Vector3> OnHit;   // 피격 시 (공격 방향 전달)

    // 프로퍼티
    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth <= 0;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>(); // 물리 컴포넌트 캐싱
        
        var controller = GetComponent<EnemyController>(); // 컨트롤러 가져오기
        if (controller?.EnemyData != null) // DataSO가 있으면
        {
            Debug.Log($"[EnemyStats] {gameObject.name}: EnemyDataSO 적용됨 - HP: {controller.EnemyData.FinalMaxHealth}");
            Initialize(controller.EnemyData); // DataSO로 초기화
        }
        else // DataSO가 없으면
        {
            Debug.Log($"[EnemyStats] {gameObject.name}: EnemyDataSO 없음 - 폴백 사용 (HP: {_maxHealth})");
            _currentHealth = _maxHealth; // 폴백 값 사용
        }
    }
    
    // EnemyDataSO 기반 초기화
    public void Initialize(EnemyDataSO data)
    {
        _maxHealth = data.FinalMaxHealth;                            // 최대 체력 설정
        _currentHealth = _maxHealth;                                 // 현재 체력 = 최대 체력
        _knockbackForce = _knockbackForce * (1f - data.knockbackResistance); // 넉백 저항 적용
    }

    // 데미지 처리 (넉백 포함)
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection)
    {
        if (IsDead) return;              // 이미 죽었으면 무시
        if (damage < 0) damage = 0;      // 음수 데미지 방지

        // Boss 취약 상태 시 데미지 배율 적용
        var bossController = GetComponent<BossController>(); // 보스인지 확인
        if (bossController != null && bossController.IsVulnerable) // 보스이고 취약 상태면
        {
            int originalDamage = damage;                                    // 원본 데미지 저장
            damage = Mathf.RoundToInt(damage * bossController.DamageMultiplier); // 배율 적용
            Debug.Log($"[Vulnerability] {name}: 취약 데미지 적용 ({originalDamage} → {damage})");
        }

        _currentHealth -= damage;                    // 체력 감소
        if (_currentHealth < 0) _currentHealth = 0;  // 0 이하 방지

        OnHealthChanged?.Invoke(); // 체력 변경 이벤트
        OnHit?.Invoke(attackDirection); // 피격 이벤트
        
        ApplyKnockback(attackDirection); // 넉백 적용

        if (IsDead) // 사망 체크
        {
            OnDeath?.Invoke(); // 사망 이벤트
        }
    }
    
    // 데미지 처리 (넉백 없음)
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position, Vector3.zero); // 기본 호출
    }
    
    // 넉백 적용
    private void ApplyKnockback(Vector3 direction)
    {
        if (_rb == null || direction == Vector3.zero) return; // 조건 미충족시 종료
        
        Vector3 knockbackDir = (direction.normalized + Vector3.up * 0.3f).normalized; // 약간 위로 튀김
        _rb.AddForce(knockbackDir * _knockbackForce, ForceMode.Impulse); // 충격력 적용
    }

    // 체력 회복
    public virtual void Heal(int healAmount)
    {
        if (IsDead) return;                                      // 죽어있으면 종료
        if (healAmount < 0) healAmount = 0;                      // 회복량이 0보다 작으면 0으로

        _currentHealth += healAmount;                            // 회복
        if (_currentHealth > _maxHealth) _currentHealth = _maxHealth; // 최대 체력 초과시 최대 체력으로

        OnHealthChanged?.Invoke();                               // 체력 변경 이벤트 발생
    }

    // 체력 리셋 (리스폰용)
    public void ResetHealth()
    {
        _currentHealth = _maxHealth; // 최대 체력으로 복구
        OnHealthChanged?.Invoke();   // 이벤트 발생
    }
    
    // 최대 체력 설정
    public void SetMaxHealth(int newMaxHealth)
    {
        _maxHealth = newMaxHealth;       // 최대 체력 변경
        _currentHealth = _maxHealth;     // 현재 체력도 변경
        OnHealthChanged?.Invoke();       // 이벤트 발생
    }
}
