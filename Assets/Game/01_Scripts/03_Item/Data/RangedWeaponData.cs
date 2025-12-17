using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Duckov/Item/Weapon/Ranged")]
public class RangedWeaponData : WeaponData
{
    [Header("Ranged Specifics")]
    public int maxAmmo;
    public float reloadTime;
    public float maxRange;
    public float bulletSpeed;

    [Header("Projectiles")]
    public GameObject bulletPrefab;
    public GameObject casingPrefab;
}