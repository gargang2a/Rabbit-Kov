using UnityEngine;

// ==========================================
// 1. 기본 아이템 데이터 (모든 아이템의 부모)
// ==========================================
public enum ItemType { Resource, Equipment, Consumable }

[CreateAssetMenu(fileName = "New Item", menuName = "Duckov/Item/BaseItem")]
public class ItemData : ScriptableObject
{
    [Header("Base Info")]
    public string id;             // 고유 ID
    public string itemName;       // 이름
    public Sprite icon;           // UI 아이콘
    public ItemType itemType;     // 타입
    [TextArea] public string description;

    [Header("Extraction Stats 무게 / 최대 중첩 수")]
    public float weight;          // 무게 (이동속도 영향)
    public int maxStackSize;      // 최대 중첩 수
}