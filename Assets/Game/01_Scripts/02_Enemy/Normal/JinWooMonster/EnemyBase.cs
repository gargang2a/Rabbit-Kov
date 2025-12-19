using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] protected int _maxHealth = 100;
    [SerializeField] protected float _knockbackForce = 5f;

    [Header("Visuals")]
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Color _damageFlashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.1f;

    // Runtime State
    protected int _currentHealth;
    protected bool _isDead = false;
    protected Rigidbody _rb;
    private Color _originalColor;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _currentHealth = _maxHealth;

        // 렌더러 자동 할당 (없으면 자식에서 찾음)
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;
    }

    // IDamageable 인터페이스 구현 - 상세 버전 (넉백 강도 포함)
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection, float knockbackForce)
    {
        if (_isDead) return;

        _currentHealth -= damage;

        // 피격 반응 (이펙트, 넉백)
        StartCoroutine(DamageFlashRoutine());
        ApplyKnockback(attackDirection, knockbackForce);

        Debug.Log($"{gameObject.name} Hit! HP: {_currentHealth}");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    
    // IDamageable 인터페이스 구현 - 중간 버전 (기본 넉백)
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection)
    {
        TakeDamage(damage, hitPoint, attackDirection, _knockbackForce);
    }
    
    // IDamageable 인터페이스 구현 - 간단 버전
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position, Vector3.zero, 0f);
    }

    protected virtual void ApplyKnockback(Vector3 direction, float knockback)
    {
        if (_rb != null && knockback > 0)
        {
            _rb.velocity = Vector3.zero;
            Vector3 knockbackDir = (direction.normalized + Vector3.up * 0.5f).normalized;
            _rb.AddForce(knockbackDir * knockback, ForceMode.Impulse);
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (_renderer != null)
        {
            _renderer.material.color = _damageFlashColor;
            yield return new WaitForSeconds(_flashDuration);
            _renderer.material.color = _originalColor;
        }
    }

    protected virtual void Die()
    {
        _isDead = true;
        // 여기에 사망 애니메이션, 아이템 드랍, 점수 추가 로직이 들어갑니다.
        Debug.Log($"{gameObject.name} Died.");

        // 임시: 2초 뒤 오브젝트 삭제
        Destroy(gameObject, 2f);
    }
}