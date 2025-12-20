// ItemData.cs 파일 수정

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
    public int price;
    public Sprite icon;           // UI 아이콘
    public ItemType itemType;     // 타입
    [TextArea] public string description;

    [Header("Settings")]
    public bool isStackable;

    [Header("Extraction Stats 무게 / 최대 중첩 수")]
    public float weight;          // 무게 (이동속도 영향)
    public int maxStackSize;      // 최대 중첩 수

    // ★★★ [신규 추가] 아이템 드랍을 위한 월드 프리팹 참조
    [Header("World Drop")]
    [Tooltip("인벤토리에서 버리거나 적이 드랍할 때 월드에 생성될 프리팹. ItemHighlighter가 부착되어 있어야 합니다.")]
    public GameObject worldPrefab; // 아이템 드랍에 필수적입니다.
}