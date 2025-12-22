using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Duckov/Item/Weapon/Ranged")]
public class RangedWeaponData : WeaponData
{
    [Header("Ranged Specifics")]
    public int maxAmmo;
    public float reloadTime;
    public float maxRange;
    public float bulletSpeed;

    [Header("Shooting Settings")]
    [Tooltip("탄퍼짐 각도")]
    [Range(0f, 45f)] public float spreadAngle = 0f;

    [Tooltip("한 번에 발사되는 총알 수 (기본 1, 샷건 5~8)")]
    [Min(1)] public int pelletCount = 1;

    [Header("Projectiles")]
    public GameObject bulletPrefab;
    public GameObject casingPrefab;

    [Header("Ammo Type")]
    [Tooltip("이 총이 사용하는 탄약 아이템 데이터 (예: 5.56mm)")]
    public ItemData ammoItemData; // ★ 필수 연결

    [Header("Effects")]
    public GameObject hitEffectPrefab;
}