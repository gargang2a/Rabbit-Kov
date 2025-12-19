using UnityEngine;

[CreateAssetMenu(fileName = "New Grenade", menuName = "Duckov/Item/Weapon/Throwable")]
public class ThrowableWeaponData : WeaponData
{
    // [중요] 부모(WeaponData)에 이미 'public int damage'가 있으므로, 여기서 또 쓰면 에러가 납니다.
    // 따라서 damage 변수는 지웠지만, 인스펙터 창의 'Combat Stats' 헤더 밑에서 데미지를 설정할 수 있습니다.

    [Header("Explosion Stats")]
    public float explosionRadius = 5f;  // 폭발 반경
    public float explosionDelay = 3f;   // 폭발 지연 시간
    public float explosionForce = 700f; // 적을 밀어내는 물리력
    public LayerMask targetLayer;       // 데미지를 입힐 대상 (Enemy 등)

    [Header("Throw Stats")]
    public float throwForce = 15f;      // 던지는 힘
    public float throwUpwardForce = 2f; // 위로 던지는 힘

    [Header("Effects")]
    public GameObject explosionEffect;  // 폭발 이펙트 프리팹
    public AudioClip explosionSound;    // 폭발 소리

    [Header("Camera Shake (Impact)")]
    public float shakeDuration = 0.5f;  // 흔들리는 시간
    public float shakeStrength = 1.0f;  // 흔들리는 강도
}