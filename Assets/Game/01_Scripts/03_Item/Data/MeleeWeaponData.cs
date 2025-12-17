// ==========================================
// 3. 근접 무기 데이터 (도끼, 칼)
// ==========================================
using UnityEngine;

[CreateAssetMenu(fileName = "New Melee", menuName = "Duckov/Item/Weapon/Melee")]
public class MeleeWeaponData : WeaponData
{
    [Header("Melee Specifics")]
    public float staminaCost;     // 공격 시 스태미너 소모
    public float attackRange;     // 공격 범위 (필요 시)
}