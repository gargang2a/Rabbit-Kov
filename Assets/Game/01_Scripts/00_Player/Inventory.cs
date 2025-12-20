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
    // UI 무게 갱신용 이벤트
    public event Action<float> OnWeightChanged;

    public List<ItemData> Items => _items;
    public float CurrentWeight => _player != null ? _player.CurrentWeight : 0f;
    public float MaxWeight => _player != null ? _player.MaxWeight : 0f;

    private void Awake()
    {
        if (_player == null)
            _player = GetComponent<Player>();
    }

    private void Start()
    {
        // 게임 시작 시 초기 무게 계산 (저장된 데이터가 있을 경우 대비)
        CalculateTotalWeight();
    }

    // ==========================================
    // 1. 아이템 추가 (줍기)
    // ==========================================
    public bool AddItem(ItemData newItem)
    {
        // 1. 슬롯 공간 확인
        if (_items.Count >= _capacity)
        {
            Debug.Log("인벤토리 칸이 부족합니다!");
            return false;
        }

        // 2. ★ [핵심 수정] 무게 제한 확인
        // 플레이어 정보가 있다면 무게를 체크합니다.
        if (_player != null)
        {
            // 현재 무게 + 새 아이템 무게가 한도를 넘는지 검사
            if (_player.CurrentWeight + newItem.weight > _player.MaxWeight)
            {
                Debug.Log($"무게 초과! (현재: {_player.CurrentWeight} + 아이템: {newItem.weight} > 한도: {_player.MaxWeight})");

                // (선택 사항) 화면 중앙에 경고 메시지 띄우기
                // UIManager.Instance.ShowWarning("가방이 너무 무겁습니다.");

                return false; // ★ 획득 실패 (아이템이 바닥에 남음)
            }
        }

        // 3. 검사 통과: 아이템 추가
        _items.Add(newItem);

        // 4. 무게 재계산 및 UI 갱신
        CalculateTotalWeight();
        OnInventoryChanged?.Invoke();

        return true; // 획득 성공
    }

    // ==========================================
    // 2. 아이템 제거 (버리기/사용)
    // ==========================================
    public bool RemoveItem(ItemData itemToRemove)
    {
        bool wasRemoved = _items.Remove(itemToRemove);

        if (wasRemoved)
        {
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

        // 1. 플레이어 데이터 갱신
        _player.UpdateWeight(totalWeight);

        // 2. UI에 즉시 이벤트 발송 (InventoryUI가 구독 중이면 텍스트 갱신됨)
        OnWeightChanged?.Invoke(totalWeight);
    }
}