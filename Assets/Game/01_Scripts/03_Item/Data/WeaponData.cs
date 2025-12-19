using UnityEngine;

// 무기 타입 정의
public enum WeaponType
{
    Melee,      // 근접 (칼, 도끼)
    Ranged,     // 원거리 (총)
    Throwable   // 투척 (수류탄, 화염병)
}

// 무기 기본 데이터 - 모든 무기의 공통 속성
public abstract class WeaponData : ItemData
{
    [Header("Weapon Visuals")]
    public GameObject weaponPrefab; // 손에 들고 있을 때 프리팹

    [Header("Weapon Type")]
    public WeaponType weaponType;   // 타입 구분용

    [Header("Combat Stats")]
    public int damage;              // 데미지
    public float coolTime;          // 쿨타임 (초)
    
    // 넉백 강도 자동 계산 (데미지 × 쿨타임 기반)
    // 높은 데미지 + 느린 연사 = 강한 넉백 (샷건, 도끼)
    // 낮은 데미지 + 빠른 연사 = 약한 넉백 (기관총)
    public float CalculatedKnockback
    {
        get
        {
            float damageComponent = damage * 0.1f;                       // 데미지 기여
            float coolTimeComponent = Mathf.Clamp(coolTime, 0.1f, 2f);   // 쿨타임 보정 (0.1~2초)
            return damageComponent * coolTimeComponent;                   // 최종 넉백
        }
    }
}
