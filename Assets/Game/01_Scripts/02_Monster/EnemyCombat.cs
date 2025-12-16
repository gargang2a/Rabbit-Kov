using UnityEngine;

// 적 전투 시스템 - 공격 사거리, 쿨다운, 데미지 처리
public class EnemyCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("공격 가능 거리 (m)")]
    [SerializeField] private float _attackRange = 1.5f;
    
    [Tooltip("공격 쿨다운 (초)")]
    [SerializeField] private float _attackCooldown = 1f;
    
    [Tooltip("공격력")]
    [SerializeField] private int _attackDamage = 10;

    private EnemyController _controller;
    private float _lastAttackTime;

    // 프로퍼티, 외부에서 읽기 전용
    public float AttackRange => _attackRange;
    public int AttackDamage => _attackDamage;

    // 컴포넌트 캐싱
    private void Awake()
    {
        _controller = GetComponent<EnemyController>();
    }

    // 공격 시도 - 조건 충족 시 IDamageable로 데미지 전달
    public void TryAttack()
    {
        if (_controller == null || _controller.CurrentTarget == null) return;

        Vector3 targetPosition = _controller.CurrentTarget.position;
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > _attackRange) return; // 사거리 밖

        float currentTime = Time.time;
        if (currentTime < _lastAttackTime + _attackCooldown) return; // 쿨다운 중

        // IDamageable 인터페이스를 통한 데미지 처리
        if (_controller.CurrentTarget.TryGetComponent(out IDamageable target))
        {
            Vector3 attackDir = (targetPosition - transform.position).normalized;
            target.TakeDamage(_attackDamage, targetPosition, attackDir);
            Debug.Log($"{gameObject.name} → {_controller.CurrentTarget.name} ({_attackDamage} damage)");
        }
        
        _lastAttackTime = currentTime;
    }

#if UNITY_EDITOR
    // 공격 사거리 시각화 (에디터 전용)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
#endif
}

