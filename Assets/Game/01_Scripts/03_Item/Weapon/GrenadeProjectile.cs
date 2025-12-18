using System.Collections;
using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    private int _damage;
    private float _explosionRadius;
    private float _explosionForce;
    private GameObject _explosionEffect;
    private AudioClip _explosionSound;

    private bool _hasExploded = false;

    // 1. 바닥에 있는 아이템일 때 (플레이어와 물리 충돌만 무시)
    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider myCol = GetComponent<Collider>();
            Collider playerCol = player.GetComponent<Collider>();
            CharacterController playerCC = player.GetComponent<CharacterController>();

            if (myCol != null)
            {
                if (playerCol != null) Physics.IgnoreCollision(playerCol, myCol);
                if (playerCC != null) Physics.IgnoreCollision(playerCC, myCol);
            }
        }
    }

    // 2. 던져진 수류탄일 때 (상호작용 끄기 + 데이터 설정 + 타이머 시작)
    public void Setup(ThrowableWeaponData data, Collider ownerCollider)
    {
        // ★ [핵심] 던져진 놈은 'Default' 레이어로 바꿔서 줍기 UI 안 뜨게 함
        gameObject.layer = LayerMask.NameToLayer("Default");

        // ★ [핵심] 줍기용 Trigger 콜라이더 끄기
        Collider[] allColliders = GetComponents<Collider>();
        foreach (Collider col in allColliders)
        {
            if (col.isTrigger) col.enabled = false;
        }

        // 데이터 주입
        _damage = data.damage;
        _explosionRadius = data.explosionRadius;
        _explosionForce = data.explosionForce;
        _explosionEffect = data.explosionEffect;
        _explosionSound = data.explosionSound;

        // 던진 사람과 충돌 무시
        Collider myCol = GetComponent<Collider>();
        if (myCol != null && ownerCollider != null)
        {
            Physics.IgnoreCollision(ownerCollider, myCol);
        }

        // ★ [오류 해결 부분] 폭발 타이머 코루틴 시작
        StartCoroutine(ExplodeRoutine(data.explosionDelay));
    }

    // ★ [오류 해결 부분] 이 함수가 지워졌거나 괄호 안에 있어서 에러가 났던 것입니다.
    private IEnumerator ExplodeRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Explode();
    }

    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        // 이펙트
        if (_explosionEffect != null)
        {
            Instantiate(_explosionEffect, transform.position, transform.rotation);
        }

        // 사운드 (2D로 크게)
        if (_explosionSound != null)
        {
            GameObject soundObj = new GameObject("GrenadeSound");
            soundObj.transform.position = transform.position;

            AudioSource audio = soundObj.AddComponent<AudioSource>();
            audio.clip = _explosionSound;
            audio.volume = 1.0f;
            audio.spatialBlend = 0f; // 2D 사운드

            audio.Play();
            Destroy(soundObj, _explosionSound.length);
        }

        // 폭발 데미지 및 넉백
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRadius);
        foreach (Collider nearbyObject in colliders)
        {
            // 데미지 (넉백 방향 0)
            if (nearbyObject.TryGetComponent(out IDamageable target))
            {
                target.TakeDamage(_damage, nearbyObject.transform.position, Vector3.zero);
            }

            // 물리적 넉백
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(_explosionForce, transform.position, _explosionRadius, 1.0f, ForceMode.Impulse);
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}