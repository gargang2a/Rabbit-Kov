using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 적 체력 관리 - 데미지, 회복, 사망 이벤트
    public class EnemyStats : MonoBehaviour
    {
        [Header("체력 설정")]
        [SerializeField] private float _maxHealth = 100f;  // 최대 체력
        
        private float _currentHealth;  // 현재 체력

        // 이벤트 (다른 스크립트에서 += 로 구독)
        public event Action OnHealthChanged;  // 체력 변경 시
        public event Action OnDeath;          // 사망 시

        public float MaxHealth { get { return _maxHealth; } }
        public float CurrentHealth { get { return _currentHealth; } }

        public bool isDead
        {
            get
            {
                if (_currentHealth <= 0) return true;
                return false;
            }
        }

        // 체력 비율 (0.0~1.0) - 체력바 UI용
        public float HealthRatio
        {
            get
            {
                if (_maxHealth > 0) return _currentHealth / _maxHealth;
                return 0f;
            }
        }

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        // 데미지 받기
        public virtual void TakeDamage(float damageAmount)
        {
            if (isDead == true) return;
            if (damageAmount < 0) damageAmount = 0;

            _currentHealth = _currentHealth - damageAmount;
            if (_currentHealth < 0) _currentHealth = 0;

            if (OnHealthChanged != null) OnHealthChanged();

            if (isDead == true)
            {
                if (OnDeath != null) OnDeath();
            }
        }

        // 회복
        public virtual void Heal(float healAmount)
        {
            if (isDead == true) return;
            if (healAmount < 0) healAmount = 0;

            _currentHealth = _currentHealth + healAmount;
            if (_currentHealth > _maxHealth) _currentHealth = _maxHealth;

            if (OnHealthChanged != null) OnHealthChanged();
        }

        // 풀피 회복 (리스폰용)
        public void ResetHealth()
        {
            _currentHealth = _maxHealth;
            if (OnHealthChanged != null) OnHealthChanged();
        }
    }
}