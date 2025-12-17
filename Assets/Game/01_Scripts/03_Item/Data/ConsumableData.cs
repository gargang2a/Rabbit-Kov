using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Duckov/Item/Consumable")]
public class ConsumableData : ItemData
{
    [Header("Consumable Effect")]
    public float healAmount;      // 체력 회복량
    public float staminaAmount;   // 스태미너 회복량
}