// Inventory.cs 파일 전체 코드 (OnWeightChanged 이벤트 추가 및 호출)

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
    // ★★★ [신규 추가] 무게 전용 이벤트 (float: totalWeight)
    public event Action<float> OnWeightChanged;

    // 외부에서 아이템 리스트를 읽을 수 있게 함
    public List<ItemData> Items => _items;

    private void Awake()
    {
        // 플레이어 참조가 비어있으면 자동 찾기
        if (_player == null)
            _player = GetComponent<Player>();
    }

    // ==========================================
    // 1. 아이템 추가 (줍기)
    // ==========================================
    public bool AddItem(ItemData newItem)
    {
        if (_items.Count >= _capacity)
        {
            Debug.Log("인벤토리 칸이 부족합니다!");
            return false;
        }

        if (_player != null)
        {
            // (무게 체크 로직 생략: Player 클래스에 의존)
            // if (_player.CurrentWeight + newItem.weight > _player.MaxWeight) { ... }
        }

        _items.Add(newItem);

        // 무게 재계산 및 UI 갱신 이벤트 호출
        CalculateTotalWeight();
        OnInventoryChanged?.Invoke();

        return true;
    }

    // ==========================================
    // 2. 아이템 제거 (버리기/사용)
    // ==========================================
    /// <summary>
    /// 인벤토리에서 아이템을 제거하고 성공 여부를 반환합니다.
    /// </summary>
    public bool RemoveItem(ItemData itemToRemove)
    {
        bool wasRemoved = _items.Remove(itemToRemove);

        if (wasRemoved)
        {
            // 제거 성공 시에만 후속 작업 실행
            // 무게 재계산 및 UI 갱신 이벤트 호출
            CalculateTotalWeight();
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    // 현재 인벤토리의 총 무게 계산 후 플레이어에게 전달 및 UI에 이벤트 발송
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

        // 1. 플레이어 데이터 갱신 (Player에 UpdateWeight(float)가 있다고 가정)
        _player.UpdateWeight(totalWeight);

        // 2. ★★★ UI에 즉시 이벤트 발송 (갱신된 무게 값을 전달)
        OnWeightChanged?.Invoke(totalWeight);
    }
}