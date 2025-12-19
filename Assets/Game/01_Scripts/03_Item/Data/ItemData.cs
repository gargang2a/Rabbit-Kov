using UnityEngine;

public enum ItemType { Resource, Equipment, Consumable }

[CreateAssetMenu(fileName = "New Item", menuName = "Duckov/Item/BaseItem")]
public class ItemData : ScriptableObject
{
    [Header("Base Info")]
    public string id;             // 고유 ID
    public string itemName;       // 이름
    public int price;
    public Sprite icon;           // UI 아이콘
    public ItemType itemType;     // 타입
    public GameObject prefab; // 아이템의 3D 프리팹 (필요 시) [cite: 9, 12]

    [TextArea(3, 5)]               // [개선] 텍스트 영역의 가독성을 높입니다.
    public string description;

    [Header("Settings")]
    public bool isStackable;

    [Header("Extraction Stats 무게 / 최대 중첩 수")]
    public float weight;
    public int maxStackSize;
}