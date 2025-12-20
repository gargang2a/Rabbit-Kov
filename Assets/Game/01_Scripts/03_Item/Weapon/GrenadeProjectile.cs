using System.Collections;
using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    private ThrowableWeaponData _data;
    private bool _hasExploded = false;

    // 투척 무기로서 생성될 때 호출되는 초기화 함수
    public void Setup(ThrowableWeaponData data, Collider ownerCollider)
    {
        _data = data;
        gameObject.layer = LayerMask.NameToLayer("Default");

        // 1. 트리거 콜라이더(줍기용) 끄기
        // 던져진 상태에서는 줍기 판정이 필요 없고, 물리 충돌만 필요함
        Collider[] allColliders = GetComponents<Collider>();
        foreach (Collider col in allColliders)
        {
            if (col.isTrigger) col.enabled = false;
        }

        // 2. 플레이어와 충돌 무시 (던지자마자 내 몸에 맞고 터지는 것 방지)
        if (ownerCollider != null)
        {
            foreach (Collider myCol in allColliders)
            {
                if (!myCol.isTrigger) Physics.IgnoreCollision(ownerCollider, myCol);
            }
        }

        // ★ [핵심 추가] 물리 설정 변경 (투척 모드)
        // 프리팹에는 Drag가 10으로 되어있지만(버리기용), 던질 때는 0.05로 바꿔서 잘 날아가게 함
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.drag = 0.05f;          // 공기 저항 제거 (멀리 날아감)
            rb.angularDrag = 0.05f;   // 회전 저항 제거 (바닥에서 데굴데굴 구름)
        }

        // 3. 트레일 렌더러 켜기
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

        // 이펙트 생성
        if (_data.explosionEffect != null)
        {
            GameObject vfx = Instantiate(_data.explosionEffect, transform.position, Quaternion.identity);
            float effectScale = _data.explosionRadius * 2f;
            vfx.transform.localScale = new Vector3(effectScale, effectScale, effectScale);
            Destroy(vfx, 3.0f);
        }

        // 사운드 재생 (2D)
        if (_data.explosionSound != null)
        {
            GameObject soundObj = new GameObject("ExplosionSound_2D");
            soundObj.transform.position = transform.position;

            AudioSource audio = soundObj.AddComponent<AudioSource>();
            audio.clip = _data.explosionSound;
            audio.volume = 1.0f;
            audio.spatialBlend = 0.0f;

            audio.Play();
            Destroy(soundObj, _data.explosionSound.length + 0.1f);
        }

        // 카메라 흔들림 (QuarterViewCamera 연동)
        if (QuarterViewCamera.Instance != null)
        {
            // 데이터에 값이 없으면 기본값(0.2초, 1.5강도) 사용 등의 예외처리 가능
            QuarterViewCamera.Instance.Shake(_data.shakeDuration, _data.shakeStrength);
        }

        // 폭발 데미지 및 물리력 적용
        int layerMask = _data.targetLayer != 0 ? _data.targetLayer : Physics.DefaultRaycastLayers;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _data.explosionRadius, layerMask);

        foreach (Collider hit in hitColliders)
        {
            if (hit.TryGetComponent(out IDamageable target))
            {
                target.TakeDamage(_data.damage);
            }

            Rigidbody targetRb = hit.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.AddExplosionForce(_data.explosionForce, transform.position, _data.explosionRadius, 1.0f, ForceMode.Impulse);
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