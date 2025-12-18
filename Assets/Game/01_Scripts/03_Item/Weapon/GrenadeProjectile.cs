using System.Collections;
using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    // 데이터는 Setup 함수로 주입받으므로 Inspector 설정 불필요 (디버깅용으로 SerializeField 유지 가능)
    private int _damage;
    private float _explosionRadius;
    private float _explosionForce;
    private GameObject _explosionEffect;
    private AudioClip _explosionSound;

    private bool _hasExploded = false;

    // ★ 외부(ThrowableWeapon)에서 데이터를 주입하는 함수
    public void Setup(ThrowableWeaponData data, Collider ownerCollider)
    {
        _damage = data.damage;
        _explosionRadius = data.explosionRadius;
        _explosionForce = data.explosionForce;
        _explosionEffect = data.explosionEffect;
        _explosionSound = data.explosionSound;

        // 플레이어(던진 사람)와 충돌 무시 처리
        Collider myCol = GetComponent<Collider>();
        if (myCol != null && ownerCollider != null)
        {
            Physics.IgnoreCollision(ownerCollider, myCol);
        }

        // 폭발 타이머 시작
        StartCoroutine(ExplodeRoutine(data.explosionDelay));
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

        // 1. 이펙트 생성
        if (_explosionEffect != null)
        {
            Instantiate(_explosionEffect, transform.position, transform.rotation);
        }

        // 2. 사운드 재생
        if (_explosionSound != null)
        {
            // 임시 오디오 객체 생성
            GameObject soundObj = new GameObject("GrenadeSound");
            soundObj.transform.position = transform.position;

            AudioSource audio = soundObj.AddComponent<AudioSource>();
            audio.clip = _explosionSound;
            audio.volume = 1.0f;
            audio.spatialBlend = 1.0f; // ★ 3D 사운드로 변경 (거리에 따라 소리 작아짐)
            audio.minDistance = 2f;
            audio.maxDistance = 20f;

            audio.Play();
            Destroy(soundObj, _explosionSound.length);
        }

        // 3. 카메라 흔들기 (CameraShake 싱글톤이 존재한다고 가정)
        // if (CameraShake.instance != null) CameraShake.instance.Shake(0.5f, 0.5f);

        // 4. 범위 데미지 및 물리력 적용
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRadius);
        foreach (Collider nearbyObject in colliders)
        {
            // 데미지 처리
            if (nearbyObject.TryGetComponent(out IDamageable target))
            {
                // 폭발 중심에서 대상까지의 방향
                Vector3 damageDir = (nearbyObject.transform.position - transform.position).normalized;

                // 거리에 따른 데미지 감쇠 (선택 사항, 현재는 고정 데미지)
                target.TakeDamage(_damage, nearbyObject.transform.position, damageDir);
            }

            // 물리력 적용 (밀쳐내기)
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(_explosionForce, transform.position, _explosionRadius);
            }
        }

        // 5. 오브젝트 삭제
        Destroy(gameObject);
    }

    // 기즈모: 에디터에서 폭발 범위 확인용
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}