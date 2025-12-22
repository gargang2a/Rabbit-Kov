using UnityEngine;
using System.Collections;

public class ThrowableWeapon : Weapon
{
    private ThrowableWeaponData _throwableData;
    private Transform _ownerFirePoint;

    // ★ [최적화] 매번 GetComponent 하지 않도록 캐싱
    private Renderer[] _cachedRenderers;

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint);
        _throwableData = data as ThrowableWeaponData;
        _ownerFirePoint = ownerFirePoint;

        if (_ownerFirePoint == null) _ownerFirePoint = transform;

        // 1. 물리 끄기 (손에 들고 있을 때)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 2. 트레일(꼬리) 끄기 - 손에 들고 있을 땐 절대 나오면 안 됨
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.enabled = false;

        // 3. 렌더러들 미리 찾아놓기 (최적화)
        _cachedRenderers = GetComponentsInChildren<Renderer>();
    }

    public override void Use()
    {
        // _isAttacking 부모 변수 사용
        if (!_isReady || _throwableData == null || _isAttacking) return;
        StartCoroutine(ThrowRoutine());
    }

    private IEnumerator ThrowRoutine()
    {
        _isAttacking = true;
        _isReady = false;

        // 1. 실제 투사체 발사
        ThrowGrenade();

        // 2. 손에 있는 모델 숨기기
        // ★ [Fix] 트레일 렌더러는 건드리지 않도록 예외 처리
        SetVisuals(false);

        yield return new WaitForSeconds(_baseData.coolTime);

        // 3. 쿨타임 끝, 다시 손에 보이게 하기
        // ★ [Fix] 여기서 트레일까지 켜지는 것을 방지함
        SetVisuals(true);

        _isReady = true;
        _isAttacking = false;
    }

    // ★ [핵심] 렌더러를 끄고 켤 때 트레일은 무시하는 함수
    private void SetVisuals(bool isActive)
    {
        if (_cachedRenderers == null) return;

        foreach (var r in _cachedRenderers)
        {
            // 만약 이 렌더러가 '트레일(이펙트)'라면 건너뛴다.
            if (r is TrailRenderer) continue;

            // 파티클 렌더러도 있다면 건너뛴다. (필요 시 주석 해제)
            // if (r is ParticleSystemRenderer) continue;

            r.enabled = isActive;
        }
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