// ==========================================
// 2. 근접 무기 로직
// ==========================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeleeWeapon : Weapon
{
    [Header("Collision")]
    [SerializeField] private Collider _hitBox; // 칼날에 붙은 콜라이더

    private MeleeWeaponData _meleeData;
    private bool _isAttacking = false;

    // ★ 수정됨: 부모 클래스와 똑같이 매개변수(Transform ownerFirePoint = null)를 추가해야 함
    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        // 부모에게도 그대로 전달
        base.Initialize(data, ownerFirePoint);

        _meleeData = data as MeleeWeaponData;

        if (_hitBox != null)
        {
            _hitBox.enabled = false; // 평소엔 꺼둠
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

        // 애니메이션 타이밍에 맞춰 콜라이더 켜기 (예시: 0.1초 뒤 켜짐)
        yield return new WaitForSeconds(0.1f);
        if (_hitBox != null) _hitBox.enabled = true;

        // 공격 지속 시간 (예시: 0.2초간 판정)
        yield return new WaitForSeconds(0.2f);
        if (_hitBox != null) _hitBox.enabled = false;

        // 쿨타임 대기
        // (안전장치: 쿨타임이 너무 짧으면 에러날 수 있으므로 0보다 큰지 체크)
        float waitTime = _baseData.coolTime - 0.3f;
        if (waitTime > 0) yield return new WaitForSeconds(waitTime);

        _isAttacking = false;
        _isReady = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 공격 중에만 데미지 판정
        if (_isAttacking && other.CompareTag("Enemy"))
        {
            Debug.Log($"{other.name}에게 {_baseData.damage} 데미지 (근접)!");
            // 추후 IDamageable 인터페이스 적용 시:
            // other.GetComponent<IDamageable>()?.TakeDamage(_baseData.damage);
        }
    }
}