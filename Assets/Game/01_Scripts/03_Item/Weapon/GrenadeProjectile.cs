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
            float effectScale = _data.explosionRadius * 2f;
            vfx.transform.localScale = new Vector3(effectScale, effectScale, effectScale);
            Destroy(vfx, 3.0f);
        }

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

        // ★ [수정됨] CameraShake 대신 QuarterViewCamera의 Shake 호출
        // _data에 shakeDuration이나 Strength가 없다면 직접 숫자를 넣으셔도 됩니다 (예: 0.2f, 1.5f)
        if (QuarterViewCamera.Instance != null)
        {
            QuarterViewCamera.Instance.Shake(_data.shakeDuration, _data.shakeStrength);
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