using System;
using UnityEngine;

// 적 체력 관리 시스템 - HP 및 사망 이벤트 관리
public class EnemyStats : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;   // 최대 체력
    private float _currentHealth;                        // 현재 체력

    // 이벤트 (다른 컴포넌트에서 구독 가능)
    public event Action OnHealthChanged;  // 체력 변동 시 발생
    public event Action OnDeath;          // 사망 시 발생

    // 프로퍼티, 외부에서 읽기 전용
    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth <= 0;

    // 체력 초기화
    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    // 데미지 처리
    public virtual void TakeDamage(float dmgAmount)
    {
        if (IsDead) return; // 이미 사망 시 종료
        if (dmgAmount < 0) dmgAmount = 0; // 음수 데미지 방지

        _currentHealth -= dmgAmount; // 체력 감소
        if (_currentHealth < 0) _currentHealth = 0; // 최소값 보정

        OnHealthChanged?.Invoke(); // 체력 변동 이벤트

        if (IsDead)
        {
            OnDeath?.Invoke(); // 사망 이벤트
        }
    }

    // 체력 회복
    public virtual void Heal(float healAmount)
    {
        if (IsDead) return; // 사망 시 회복 불가
        if (healAmount < 0) healAmount = 0; // 음수 회복량 방지

        _currentHealth += healAmount; // 체력 증가
        if (_currentHealth > _maxHealth) _currentHealth = _maxHealth; // 최대값 보정

        OnHealthChanged?.Invoke(); // 체력 변동 이벤트
    }

    // 체력 리셋 (리스폰용)
    public void ResetHealth()
    {
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke();
    }
}
