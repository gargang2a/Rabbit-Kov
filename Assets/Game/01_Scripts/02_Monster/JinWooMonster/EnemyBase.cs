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

    // IDamageable 인터페이스 구현
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection)
    {
        if (_isDead) return;

        _currentHealth -= damage;

        // 피격 반응 (이펙트, 넉백)
        StartCoroutine(DamageFlashRoutine());
        ApplyKnockback(attackDirection);

        // 디버그용 로그
        Debug.Log($"{gameObject.name} Hit! HP: {_currentHealth}");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void ApplyKnockback(Vector3 direction)
    {
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero; // 기존 속도 초기화
            // Y축 넉백을 살짝 주어 튀어오르는 느낌 추가
            Vector3 knockbackDir = (direction.normalized + Vector3.up * 0.5f).normalized;
            _rb.AddForce(knockbackDir * _knockbackForce, ForceMode.Impulse);
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