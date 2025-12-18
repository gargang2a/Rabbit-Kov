using System.Collections.Generic;
using UnityEngine;
using System;

public class Inventory : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _capacity = 20; // 인벤토리 칸 수

    // 플레이어 참조 (무게 정보 확인 및 전달용)
    [SerializeField] private Player _player;

    // 실제 아이템이 담길 리스트
    private List<ItemData> _items = new List<ItemData>();

    // UI에게 "인벤토리 변했어!"라고 알려줄 이벤트
    public event Action OnInventoryChanged;

    // 외부에서 아이템 리스트를 읽을 수 있게 함
    public List<ItemData> Items => _items;

    private void Awake()
    {
        // 플레이어 참조가 비어있으면 자동 찾기
        if (_player == null)
            _player = GetComponent<Player>();
    }

    // ==========================================
    // 1. 아이템 추가 (줍기) - [핵심 수정됨]
    // ==========================================
    public bool AddItem(ItemData newItem)
    {
        // 1. 칸 수 체크
        if (_items.Count >= _capacity)
        {
            Debug.Log("인벤토리 칸이 부족합니다!");
            return false;
        }

        // ★ 2. 무게 체크 (여기가 추가된 부분)
        if (_player != null)
        {
            // (현재 무게 + 새로 들어올 아이템 무게)가 (최대 무게)보다 크다면?
            if (_player.CurrentWeight + newItem.weight > _player.MaxWeight)
            {
                Debug.Log("너무 무거워서 들 수 없습니다!");
                return false; // 줍기 실패 처리
            }
        }

        // 3. 아이템 추가 성공
        _items.Add(newItem);

        // 무게 재계산 및 UI 갱신
        CalculateTotalWeight();
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

            // 무게 재계산 및 UI 갱신
            CalculateTotalWeight();
            OnInventoryChanged?.Invoke();
        }
    }

    // 현재 인벤토리의 총 무게 계산 후 플레이어에게 전달
    private void CalculateTotalWeight()
    {
        if (_player == null) return;

        float totalWeight = 0f;
        foreach (var item in _items)
        {
            if (item != null)
            {
                totalWeight += item.weight;
            }
        }

        _player.UpdateWeight(totalWeight);
    }
}