using UnityEngine;
using System.Collections;

public class ThrowableWeapon : Weapon
{
    private ThrowableWeaponData _throwableData;
    private Transform _ownerFirePoint;
    private bool _isThrowing = false;

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint);
        _throwableData = data as ThrowableWeaponData;
        _ownerFirePoint = ownerFirePoint;

        if (_ownerFirePoint == null) _ownerFirePoint = transform;

        // 1. 손에 들고 있을 때 물리 끄기
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // ★ [핵심 수정] 손에 들고 있을 때는 트레일(꼬리) 효과 끄기!
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.enabled = false; // 아예 컴포넌트를 끄거나
            // trail.emitting = false; // 꼬리 생성만 멈추거나
        }
    }

    public override void Use()
    {
        if (!_isReady || _throwableData == null || _isThrowing) return;
        StartCoroutine(ThrowRoutine());
    }

    private IEnumerator ThrowRoutine()
    {
        _isThrowing = true;
        _isReady = false;

        ThrowGrenade();

        // 던지는 순간 손에 있는 모델 숨기기
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = false;

        yield return new WaitForSeconds(_baseData.coolTime);

        // 쿨타임 끝나면 다시 보이기
        foreach (var r in renderers) r.enabled = true;

        _isReady = true;
        _isThrowing = false;
    }

    private void ThrowGrenade()
    {
        // 1. 투사체 생성 (여기서 생성된 놈은 Prefab 원본 설정을 따르므로 Trail이 켜져 있음!)
        GameObject grenadeObj = Instantiate(_throwableData.weaponPrefab, _ownerFirePoint.position, _ownerFirePoint.rotation);

        // 2. 투사체 로직 설정
        GrenadeProjectile projectile = grenadeObj.GetComponent<GrenadeProjectile>();
        if (projectile == null) projectile = grenadeObj.AddComponent<GrenadeProjectile>();

        if (projectile != null)
        {
            Collider playerCollider = GetComponentInParent<CharacterController>();
            if (playerCollider == null) playerCollider = GetComponentInParent<Collider>();
            projectile.Setup(_throwableData, playerCollider);
        }

        // 3. 물리 힘 가하기
        Rigidbody rb = grenadeObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            Vector3 throwDir = _ownerFirePoint.forward * _throwableData.throwForce
                             + Vector3.up * _throwableData.throwUpwardForce;

            rb.AddForce(throwDir, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }
    }
}