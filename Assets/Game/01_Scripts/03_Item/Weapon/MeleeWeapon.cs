// 근접 무기 로직 - 공격 시 히트박스 활성화, 적 데미지 처리
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeleeWeapon : Weapon
{
    [Header("Collision")]
    [SerializeField] private Collider _hitBox; // 칼날에 붙은 콜라이더

    private MeleeWeaponData _meleeData;
    private bool _isAttacking = false;
    private HashSet<Collider> _hitEnemies = new HashSet<Collider>(); // 이미 타격한 적 (중복 방지)

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint);

        _meleeData = data as MeleeWeaponData;

        if (_hitBox != null)
        {
            _hitBox.enabled = false;  // 평소엔 꺼둠
            _hitBox.isTrigger = true; // 트리거 필수 설정
        }
    }

    public override void Use()
    {
        if (!_isReady || _isAttacking) return;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _isReady = false;
        _hitEnemies.Clear(); // 타격 리스트 초기화

        // 애니메이션 타이밍에 맞춰 콜라이더 켜기 (예: 0.1초 뒤)
        yield return new WaitForSeconds(0.1f);
        if (_hitBox != null) _hitBox.enabled = true;

        // 공격 지속 시간 (예: 0.2초간 판정)
        yield return new WaitForSeconds(0.2f);
        if (_hitBox != null) _hitBox.enabled = false;

        // 쿨타임 대기
        float waitTime = _baseData.coolTime - 0.3f;
        if (waitTime > 0) yield return new WaitForSeconds(waitTime);

        _isAttacking = false;
        _isReady = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 공격 중이고 적이면 데미지 처리
        if (_isAttacking && IsEnemy(other.gameObject))
        {
            // 중복 타격 방지
            if (_hitEnemies.Contains(other)) return;
            _hitEnemies.Add(other);
            
            IDamageable target = other.GetComponent<IDamageable>();
            
            // 자신에게 없으면 부모에서 찾기
            if (target == null)
            {
                target = other.GetComponentInParent<IDamageable>();
            }
            
            if (target != null)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 attackDir = (other.transform.position - transform.position).normalized;
                float knockback = _baseData.CalculatedKnockback;
                target.TakeDamage(_baseData.damage, hitPoint, attackDir, knockback);
                Debug.Log($"[MeleeWeapon] {other.name}에게 {_baseData.damage} 데미지! (넉백: {knockback:F1})");
            }
        }
    }
    
    // 적 판정: 태그 또는 레이어
    private bool IsEnemy(GameObject obj)
    {
        // 태그 체크
        if (obj.CompareTag("Enemy")) return true;
        
        // 레이어 체크 (레이어 이름: "Enemy")
        if (obj.layer == LayerMask.NameToLayer("Enemy")) return true;
        
        return false;
    }
}