using System.Collections;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [Header("Settings")]
    public float delay = 3f;
    public float explosionRadius = 5f;
    public int damage = 50;
    public float explosionForce = 700f;

    [Header("Effects")]
    public GameObject explosionEffect;
    public AudioClip explosionSound;

    private bool hasExploded = false;

    void Start()
    {
        // 1. 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // 2. 플레이어와 수류탄끼리 충돌 무시 설정
        if (player != null)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            Collider myCollider = GetComponent<Collider>();

            if (playerCollider != null && myCollider != null)
            {
                Physics.IgnoreCollision(playerCollider, myCollider);
            }
        }

        // 폭발 코루틴 시작
        StartCoroutine(ExplodeCoroutine());
    }

    IEnumerator ExplodeCoroutine()
    {
        yield return new WaitForSeconds(delay);
        Explode();
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // 1. 이펙트 생성
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, transform.rotation);
        }

        // 2. ★ [수정됨] 소리 크게 재생하기 (2D 사운드 강제 적용)
        if (explosionSound != null)
        {
            // (1) 임시 오브젝트 생성
            GameObject soundObj = new GameObject("GrenadeSound");
            soundObj.transform.position = transform.position;

            // (2) 오디오 소스 추가 및 설정
            AudioSource audio = soundObj.AddComponent<AudioSource>();
            audio.clip = explosionSound;
            audio.volume = 1.0f;       // 볼륨 최대
            audio.spatialBlend = 0f;   // ★ 0이면 2D(거리무시), 1이면 3D(거리비례)

            // (3) 재생 및 자동 파괴
            audio.Play();
            Destroy(soundObj, explosionSound.length);
        }

        // 3. 범위 데미지 처리
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider nearbyObject in colliders)
        {
            if (nearbyObject.TryGetComponent(out IDamageable target))
            {
                Vector3 damageDir = (nearbyObject.transform.position - transform.position).normalized;
                target.TakeDamage(damage, nearbyObject.transform.position, damageDir);
            }

            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        // 4. 수류탄 오브젝트 삭제
        Destroy(gameObject);
    }
}