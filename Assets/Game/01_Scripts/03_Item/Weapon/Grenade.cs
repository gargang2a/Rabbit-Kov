using System.Collections;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [Header("Settings")]
    public float delay = 3f;
    public float explosionRadius = 15f;
    public int damage = 50;
    public float explosionForce = 700f;

    [Header("Effects")]
    public GameObject explosionEffect;
    public AudioClip explosionSound;

    private bool hasExploded = false;

    void Start()
    {
        // ★ [추가됨] 플레이어와 충돌 무시 (휘어짐 방지)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider playerCol = player.GetComponent<Collider>();
            Collider myCol = GetComponent<Collider>();

            // 플레이어에게 CharacterController가 있다면 그 콜라이더도 가져옴
            CharacterController playerController = player.GetComponent<CharacterController>();

            if (myCol != null)
            {
                if (playerCol != null) Physics.IgnoreCollision(playerCol, myCol);
                if (playerController != null) Physics.IgnoreCollision(playerController, myCol);
            }
        }

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

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, transform.rotation);
        }

        if (explosionSound != null)
        {
            GameObject soundObj = new GameObject("GrenadeSound");
            soundObj.transform.position = transform.position;
            AudioSource audio = soundObj.AddComponent<AudioSource>();
            audio.clip = explosionSound;
            audio.volume = 1.0f;
            audio.spatialBlend = 0f; // 2D 사운드
            audio.Play();
            Destroy(soundObj, explosionSound.length);
        }

        // ★ [추가] 카메라 흔들기 (0.5초 동안, 강도 0.5)
        if (CameraShake.instance != null)
            CameraShake.instance.Shake(0.5f, 0.5f);

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

        Destroy(gameObject);
    }
}