using System.Collections;
using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    private ThrowableWeaponData _data;
    private bool _hasExploded = false;

    public void Setup(ThrowableWeaponData data, Collider ownerCollider)
    {
        _data = data;
        gameObject.layer = LayerMask.NameToLayer("Default");

        Collider[] allColliders = GetComponents<Collider>();
        foreach (Collider col in allColliders)
        {
            if (col.isTrigger) col.enabled = false;
        }

        if (ownerCollider != null)
        {
            foreach (Collider myCol in allColliders)
            {
                if (!myCol.isTrigger) Physics.IgnoreCollision(ownerCollider, myCol);
            }
        }

        // ★ 날아가는 놈은 트레일이 보여야 하므로 혹시 꺼져있다면 켜주기
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.enabled = true;
        }

        StartCoroutine(ExplodeRoutine(_data.explosionDelay));
    }

    private IEnumerator ExplodeRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Explode();
    }

    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        if (_data.explosionEffect != null)
        {
            GameObject vfx = Instantiate(_data.explosionEffect, transform.position, Quaternion.identity);

            // ★ [VFX 크기 조절] 
            // 기본적으로 반경(Radius)에 맞춰 스케일을 키웁니다.
            // 만약 프리팹이 원래 컸다면 _data.explosionRadius * 0.5f 처럼 보정값을 곱해주세요.
            float effectScale = _data.explosionRadius * 2f; // 지름(Diameter) 기준으로 맞추는 경우가 많습니다.
            vfx.transform.localScale = new Vector3(effectScale, effectScale, effectScale);

            Destroy(vfx, 3.0f);
        }

        // ★ [핵심 수정] 2D 사운드로 재생하기
        if (_data.explosionSound != null)
        {
            // 1. 소리를 재생할 빈 오브젝트 생성
            GameObject soundObj = new GameObject("ExplosionSound_2D");
            soundObj.transform.position = transform.position;

            // 2. AudioSource 세팅
            AudioSource audio = soundObj.AddComponent<AudioSource>();
            audio.clip = _data.explosionSound;
            audio.volume = 1.0f;
            audio.spatialBlend = 0.0f; // ★ 0이면 2D, 1이면 3D입니다.

            // 3. 재생 및 삭제 예약
            audio.Play();
            Destroy(soundObj, _data.explosionSound.length + 0.1f);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(_data.shakeDuration, _data.shakeStrength);
        }

        int layerMask = _data.targetLayer != 0 ? _data.targetLayer : Physics.DefaultRaycastLayers;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _data.explosionRadius, layerMask);

        foreach (Collider hit in hitColliders)
        {
            if (hit.TryGetComponent(out IDamageable target))
            {
                target.TakeDamage(_data.damage);
            }

            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(_data.explosionForce, transform.position, _data.explosionRadius, 1.0f, ForceMode.Impulse);
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (_data != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _data.explosionRadius);
        }
    }
}