// ==========================================
// 2. 무기 기본 데이터 (추상 클래스)
// ==========================================
using UnityEngine;

public abstract class WeaponData : ItemData
{
    [Header("Weapon Visuals 무기 프리펩")]
    public GameObject weaponPrefab; // 손에 들릴 실제 프리팹

    [Header("Combat Stats 데미지 / 연사속도")]
    public int damage;            // 데미지
    public float coolTime;        // 공격 속도 (연사 간격)
}