// ==========================================
// 3. 투척 무기 데이터
// ==========================================
using UnityEngine;

[CreateAssetMenu(fileName = "New Grenade", menuName = "Duckov/Item/Weapon/Throwable")]
public class ThrowableWeaponData : WeaponData
{
    [Header("Explosion Stats")]
    public float explosionRadius = 5f;  // 폭발 반경
    public float explosionDelay = 3f;   // 폭발 지연 시간
    public float explosionForce = 700f; // 물리력

    [Header("Throw Stats")]
    public float throwForce = 15f;      // 던지는 힘
    public float throwUpwardForce = 2f; // 위로 던지는 힘 (포물선)

    [Header("Effects")]
    public GameObject explosionEffect;  // 폭발 이펙트 프리팹
    public AudioClip explosionSound;    // 폭발 소리
}