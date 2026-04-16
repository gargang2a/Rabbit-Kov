using System.Collections.Generic;
using UnityEngine;
using System;

public class Inventory : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _capacity = 20;
    [SerializeField] private Player _player;

    private List<ItemData> _items = new List<ItemData>();

    public event Action OnInventoryChanged;
    public event Action<float> OnWeightChanged;
    public event Action<ItemData> OnItemAdded;

    // ★ [추가] 아이템이 인벤토리에서 제거될 때 알리는 이벤트
    public event Action<ItemData> OnItemRemoved;

    public List<ItemData> Items => _items;
    public float CurrentWeight => _player != null ? _player.CurrentWeight : 0f;
    public float MaxWeight => _player != null ? _player.MaxWeight : 0f;

    private void Awake()
    {
        if (_player == null) _player = GetComponent<Player>();
    }

    private void Start()
    {
        CalculateTotalWeight();
    }

    public bool AddItem(ItemData newItem)
    {
        if (_items.Count >= _capacity) return false;

        if (_player != null)
        {
            if (_player.CurrentWeight + newItem.weight > _player.MaxWeight) return false;
        }

        _items.Add(newItem);
        CalculateTotalWeight();
        OnInventoryChanged?.Invoke();
        OnItemAdded?.Invoke(newItem); // 획득 알림
        if (QuestHUDView.Instance != null)
        {
            QuestHUDView.Instance.PickUpItem(newItem.itemName);
        }

        return true;
    }

    public bool RemoveItem(ItemData itemToRemove)
    {
        string removedItemName = itemToRemove.itemName;
        bool wasRemoved = _items.Remove(itemToRemove);

        if (wasRemoved)
        {
            CalculateTotalWeight();
            OnInventoryChanged?.Invoke();

            OnItemRemoved?.Invoke(itemToRemove);
            if (QuestHUDView.Instance != null)
            {
                QuestHUDView.Instance.PickUpItem(removedItemName);
            }
            return true;
        }
        return false;
    }

    private void CalculateTotalWeight()
    {
        if (_player == null) return;
        float totalWeight = 0f;
        foreach (var item in _items)
        {
            if (item != null) totalWeight += item.weight;
        }
        _player.UpdateWeight(totalWeight);
        OnWeightChanged?.Invoke(totalWeight);
    }
}