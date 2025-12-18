using UnityEngine;

public class ThrowableWeapon : Weapon
{
    private ThrowableWeaponData _throwableData;
    private Transform _ownerFirePoint;

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint);
        _throwableData = data as ThrowableWeaponData;
        _ownerFirePoint = ownerFirePoint;

        if (_ownerFirePoint == null) _ownerFirePoint = transform;

        // ================================================================
        // ★ [핵심 수정] 손에 들려있는 동안에는 물리/충돌을 끕니다.
        // ================================================================
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // 중력 영향 안 받음, 물리 연산 안 함
            rb.useGravity = false;
            rb.velocity = Vector3.zero; // 혹시 모를 가속도 초기화
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false; // 손에 들고 있을 때 플레이어 몸이랑 부딪히지 않게 끔
        }
    }

    public override void Use()
    {
        if (!_isReady || _throwableData == null) return;

        // 투척 로직 실행
        ThrowGrenade();

        // 쿨타임 적용
        StartCoroutine(CoolTimeRoutine());
    }

    private void ThrowGrenade()
    {
        // 1. 투사체 생성 (손 위치에서 새로운 복사본 생성)
        // ★ 여기서 생성되는 녀석은 프리팹 원본 설정을 따르므로 Rigidbody가 켜져 있습니다.
        GameObject grenadeObj = Instantiate(_throwableData.weaponPrefab, _ownerFirePoint.position, _ownerFirePoint.rotation);

        // 2. 데이터 주입
        GrenadeProjectile projectile = grenadeObj.GetComponent<GrenadeProjectile>();

        // 만약 프리팹에 GrenadeProjectile이 안 붙어있다면 안전장치로 붙여줌
        if (projectile == null) projectile = grenadeObj.AddComponent<GrenadeProjectile>();

        if (projectile != null)
        {
            Collider playerCollider = GetComponentInParent<CharacterController>();
            if (playerCollider == null) playerCollider = GetComponentInParent<Collider>();

            projectile.Setup(_throwableData, playerCollider);
        }

        // 3. 물리 힘 가하기 (던지기)
        Rigidbody rb = grenadeObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // ★ 날아가는 녀석은 물리가 켜져 있어야 함 (프리팹 기본값이 켜져있겠지만 확실하게 처리)
            rb.isKinematic = false;
            rb.useGravity = true;

            Vector3 throwDir = _ownerFirePoint.forward * _throwableData.throwForce
                             + Vector3.up * _throwableData.throwUpwardForce;

            rb.AddForce(throwDir, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }
    }

    private System.Collections.IEnumerator CoolTimeRoutine()
    {
        _isReady = false;

        // (선택 사항) 던지는 순간 손에 있는 모델을 잠시 숨겼다가 쿨타임 끝나면 다시 보여주기?
        // MeshRenderer mesh = GetComponent<MeshRenderer>();
        // if(mesh) mesh.enabled = false;

        yield return new WaitForSeconds(_baseData.coolTime);

        // if(mesh) mesh.enabled = true;
        _isReady = true;
    }
}