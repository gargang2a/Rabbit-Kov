using System.Collections.Generic;
using UnityEngine;
using System;

public class Inventory : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _capacity = 20; // 인벤토리 칸 수

    // 실제 아이템이 담길 리스트
    private List<ItemData> _items = new List<ItemData>();

    // ★ UI에게 "인벤토리 변했어!"라고 알려줄 이벤트
    public event Action OnInventoryChanged;

    // 외부에서 아이템 리스트를 읽을 수 있게 함
    public List<ItemData> Items => _items;

    // ==========================================
    // 1. 아이템 추가 (줍기)
    // ==========================================
    public bool AddItem(ItemData newItem)
    {
        if (_items.Count >= _capacity)
        {
            Debug.Log("인벤토리가 가득 찼습니다!");
            return false; // 꽉 차서 못 넣음
        }

        _items.Add(newItem);

        // UI 갱신 알림
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ==========================================
    // 2. 아이템 제거 (버리기/사용)
    // ==========================================
    public void RemoveItem(ItemData itemToRemove)
    {
        if (_items.Contains(itemToRemove))
        {
            _items.Remove(itemToRemove);
            OnInventoryChanged?.Invoke();
        }
    }
}