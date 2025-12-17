using UnityEngine;

public enum ItemType { Resource, Equipment, Consumable }

[CreateAssetMenu(fileName = "New Item", menuName = "Duckov/Item/BaseItem")]
public class ItemData : ScriptableObject
{
    [Header("Base Info")]
    public string id;
    public string itemName;
    public Sprite icon;
    public ItemType itemType;
    [TextArea] public string description;

    [Header("Extraction Stats")]
    public float weight;
    public int maxStackSize;
}