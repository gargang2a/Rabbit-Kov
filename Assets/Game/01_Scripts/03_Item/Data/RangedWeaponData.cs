using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Duckov/Item/Weapon/Ranged")]
public class RangedWeaponData : WeaponData
{
    [Header("Ranged Specifics")]
    public int maxAmmo;
    public float reloadTime;
    public float maxRange;
    public float bulletSpeed;

    [Header("Shooting Settings (New)")]
    [Tooltip("탄퍼짐 각도 (0이면 정확, 숫자가 클수록 많이 빗나감)")]
    [Range(0f, 45f)] public float spreadAngle = 0f; // ★ 추가됨

    [Tooltip("한 번에 발사되는 총알 수 (기본 1, 샷건 5~8)")]
    [Min(1)] public int pelletCount = 1;            // ★ 추가됨

    [Header("Projectiles")]
    public GameObject bulletPrefab;
    public GameObject casingPrefab;
}