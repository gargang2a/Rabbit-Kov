using UnityEngine;

// ==========================================
// 1. 기본 아이템 데이터
// ==========================================
public enum ItemType { Resource, Equipment, Consumable, Ammo }

[CreateAssetMenu(fileName = "New Item", menuName = "Duckov/Item/BaseItem")]
public class ItemData : ScriptableObject
{
    [Header("Base Info")]
    public string id;
    public string itemName;
    public Sprite icon;
    public ItemType itemType;
    [TextArea] public string description;

    [Header("Settings")]
    public bool isStackable;

    [Header("Extraction Stats")]
    public float weight;
    public int maxStackSize;

    [Header("World Drop")]
    [Tooltip("월드 생성용 프리팹 (ItemPickup 스크립트 부착 필수)")]
    public GameObject worldPrefab;

    [Header("Trade Settings")]
    public int price = 100;
}