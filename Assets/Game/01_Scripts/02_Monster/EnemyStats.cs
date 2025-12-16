using System;
using UnityEngine;

// 적 체력 관리 시스템 - HP, 데미지 처리, 사망 이벤트 (IDamageable 구현)
public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 100;   // 최대 체력 (int로 통일)
    private int _currentHealth;                       // 현재 체력

    [Header("Combat Feedback")]
    [Tooltip("피격 시 넉백 강도")]
    [SerializeField] private float _knockbackForce = 3f;
    
    private Rigidbody _rb;

    // 이벤트 (다른 컴포넌트에서 구독 가능)
    public event Action OnHealthChanged;  // 체력 변동 시 발생
    public event Action OnDeath;          // 사망 시 발생
    public event Action<Vector3> OnHit;   // 피격 시 공격 방향 전달

    // 프로퍼티, 외부에서 읽기 전용
    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth <= 0;

    // 체력 초기화
    private void Awake()
    {
        _currentHealth = _maxHealth;
        _rb = GetComponent<Rigidbody>();
    }

    // IDamageable - 상세 버전 (넉백 포함)
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection)
    {
        if (IsDead) return;
        if (damage < 0) damage = 0;

        _currentHealth -= damage;
        if (_currentHealth < 0) _currentHealth = 0;

        OnHealthChanged?.Invoke();
        OnHit?.Invoke(attackDirection);
        
        // 넉백 적용
        ApplyKnockback(attackDirection);

        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }
    
    // IDamageable - 간단 버전 (넉백 없음)
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position, Vector3.zero);
    }
    
    // 넉백 처리
    private void ApplyKnockback(Vector3 direction)
    {
        if (_rb == null || direction == Vector3.zero) return;
        
        Vector3 knockbackDir = (direction.normalized + Vector3.up * 0.3f).normalized;
        _rb.AddForce(knockbackDir * _knockbackForce, ForceMode.Impulse);
    }

    // 체력 회복
    public virtual void Heal(int healAmount)
    {
        if (IsDead) return;
        if (healAmount < 0) healAmount = 0;

        _currentHealth += healAmount;
        if (_currentHealth > _maxHealth) _currentHealth = _maxHealth;

        OnHealthChanged?.Invoke();
    }

    // 체력 리셋 (리스폰용)
    public void ResetHealth()
    {
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke();
    }
}

