using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    public class EnemyStats : MonoBehaviour
    {
        [SerializeField] private float _maxHealth = 100f;
        private float _currentHealth;

        public event Action<float, float> OnHealthChange;
        public event Action OnDeath;

        public float MaxHealth { get { return _maxHealth; } }

        public float CurrentHealth { get { return _currentHealth; } }

        public bool isDead { get { return _currentHealth <= 0; } }

        public float HealthRatio
        {
            get
            {
                if (_maxHealth > 0)
                {
                    return _currentHealth / _maxHealth;
                }
                else
                {
                    return 0f;
                }
            }
        }

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        public virtual void TakeDamage(float amt)
        {
            if (isDead) return;
            if (amt < 0) amt = 0;

            _currentHealth -= amt;

            if (_currentHealth < 0) _currentHealth = 0;

            if (OnHealthChange != null)
            {
                OnHealthChange(_currentHealth, _maxHealth);
            }

            if (isDead)
            {
                if (OnDeath  != null)
                {
                    OnDeath();
                }
            }
        }

        public virtual void Heal(float amt)
        {
            if (isDead) return;
            if (amt < 0) amt = 0;

            _currentHealth += amt;

            if (_currentHealth > _maxHealth)
            {
                _currentHealth = _maxHealth;
            }

            if (OnHealthChange != null)
            {
                OnHealthChange(_currentHealth, _maxHealth);
            }
        }

        public void ResetHealth()
        {
            _currentHealth = _maxHealth;

            if (OnHealthChange != null)
            {
                OnHealthChange(_currentHealth, _maxHealth);
            }
        }
    }
}