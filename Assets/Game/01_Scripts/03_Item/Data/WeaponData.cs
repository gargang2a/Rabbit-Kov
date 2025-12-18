using UnityEngine;

// ==========================================
// 1. 무기 타입 정의 (확장성 확보)
// ==========================================
public enum WeaponType
{
    Melee,      // 근접 (칼, 도끼)
    Ranged,     // 원거리 (총)
    Throwable   // 투척 (수류탄, 화염병)
}

// ==========================================
// 2. 무기 기본 데이터 (수정됨)
// ==========================================
public abstract class WeaponData : ItemData
{
    [Header("Weapon Visuals")]
    public GameObject weaponPrefab; // 손에 들고 있을 때, 혹은 던져질 프리팹

    [Header("Weapon Type")]
    public WeaponType weaponType;   // ★ 타입 구분용

    [Header("Combat Stats")]
    public int damage;
    public float coolTime;
}
