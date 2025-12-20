using UnityEngine;
using System.Collections;

public class ThrowableWeapon : Weapon
{
    private ThrowableWeaponData _throwableData;
    private Transform _ownerFirePoint;
    // private bool _isThrowing = false; // ★ [삭제] _isAttacking으로 통합

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint);
        _throwableData = data as ThrowableWeaponData;
        _ownerFirePoint = ownerFirePoint;

        if (_ownerFirePoint == null) _ownerFirePoint = transform;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.enabled = false;
    }

    public override void Use()
    {
        // _isAttacking을 사용하여 상태 체크 (부모 변수)
        if (!_isReady || _throwableData == null || _isAttacking) return;
        StartCoroutine(ThrowRoutine());
    }

    private IEnumerator ThrowRoutine()
    {
        _isAttacking = true; // ★ 던지는 중 상태
        _isReady = false;

        ThrowGrenade();

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = false;

        yield return new WaitForSeconds(_baseData.coolTime);

        foreach (var r in renderers) r.enabled = true;

        _isReady = true;
        _isAttacking = false; // ★ 투척 완료
    }

    private void ThrowGrenade()
    {
        GameObject grenadeObj = Instantiate(_throwableData.weaponPrefab, _ownerFirePoint.position, _ownerFirePoint.rotation);

        GrenadeProjectile projectile = grenadeObj.GetComponent<GrenadeProjectile>();
        if (projectile == null) projectile = grenadeObj.AddComponent<GrenadeProjectile>();

        if (projectile != null)
        {
            Collider playerCollider = GetComponentInParent<CharacterController>();
            if (playerCollider == null) playerCollider = GetComponentInParent<Collider>();
            projectile.Setup(_throwableData, playerCollider);
        }

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