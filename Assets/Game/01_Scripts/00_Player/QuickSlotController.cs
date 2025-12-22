using System;
using UnityEngine;

public class QuickSlotController : MonoBehaviour
{
    [Header("Settings / 설정")]
    [SerializeField, Tooltip("퀵슬롯의 개수입니다. (예: 4 = 1~4번 슬롯)")]
    private int _slotCount = 4;

    [Header("References / 참조")]
    [SerializeField, Tooltip("플레이어의 무기 제어기. 설정되어 있지 않으면 Awake에서 자동 할당 시도합니다.")]
    private PlayerWeaponController _weaponController;
    [SerializeField, Tooltip("플레이어 인벤토리 참조. 설정되어 있지 않으면 Awake에서 자동 할당 시도합니다.")]
    private Inventory _inventory;

    private ItemData[] _quickSlots;
    private int _currentSlotIndex = -1;

    public event Action<int, ItemData> OnQuickSlotChanged;
    public event Action<int> OnSlotUsed;

    private void Awake()
    {
        _quickSlots = new ItemData[_slotCount];
        if (_weaponController == null) _weaponController = GetComponent<PlayerWeaponController>();
        if (_inventory == null) _inventory = GetComponent<Inventory>();
    }

    private void Start()
    {
        if (_inventory != null)
        {
            _inventory.OnItemAdded += HandleItemAdded;
            // ★ [추가] 아이템 제거 이벤트 구독
            _inventory.OnItemRemoved += HandleItemRemoved;
        }
    }

    private void OnDestroy()
    {
        if (_inventory != null)
        {
            _inventory.OnItemAdded -= HandleItemAdded;
            // ★ [추가] 구독 해제
            _inventory.OnItemRemoved -= HandleItemRemoved;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) HandleInput(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) HandleInput(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) HandleInput(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) HandleInput(3);
    }

    // 아이템 획득 시 자동 등록
    private void HandleItemAdded(ItemData newItem)
    {
        if (newItem is WeaponData)
        {
            if (IsItemRegistered(newItem)) return;

            int emptyIndex = -1;
            for (int i = 0; i < _quickSlots.Length; i++)
            {
                if (_quickSlots[i] == null)
                {
                    emptyIndex = i;
                    break;
                }
            }

            if (emptyIndex != -1)
            {
                RegisterItem(emptyIndex, newItem);
            }
        }
    }

    // ★ [추가] 아이템 제거 시 퀵슬롯 동기화 로직
    private void HandleItemRemoved(ItemData removedItem)
    {
        // 퀵슬롯 전체를 순회하며 삭제된 아이템이 있는지 확인
        for (int i = 0; i < _quickSlots.Length; i++)
        {
            if (_quickSlots[i] == removedItem)
            {
                // 1. 만약 현재 손에 들고 있는 무기를 버린 것이라면? -> 장착 해제
                if (_currentSlotIndex == i)
                {
                    _weaponController.UnequipWeapon();
                    _currentSlotIndex = -1;
                    OnSlotUsed?.Invoke(-1); // UI 선택 효과 해제
                }

                // 2. 퀵슬롯 데이터 비우기 (UI 아이콘 사라짐)
                RegisterItem(i, null);

                Debug.Log($"[QuickSlot] 인벤토리에서 제거된 아이템({removedItem.itemName})을 {i + 1}번 슬롯에서 해제했습니다.");
            }
        }
    }

    private void HandleInput(int targetIndex)
    {
        if (InventoryUI.Instance != null && InventoryUI.Instance.HoveredSlot != null)
        {
            ItemData itemToRegister = InventoryUI.Instance.HoveredSlot.Item;
            if (itemToRegister != null)
            {
                int existingIndex = GetSlotIndex(itemToRegister);
                if (existingIndex != -1) return; // 중복 방지

                RegisterItem(targetIndex, itemToRegister);
            }
            return;
        }

        UseSlot(targetIndex);
    }

    private int GetSlotIndex(ItemData item)
    {
        for (int i = 0; i < _quickSlots.Length; i++)
        {
            if (_quickSlots[i] == item) return i;
        }
        return -1;
    }

    private bool IsItemRegistered(ItemData item) => GetSlotIndex(item) != -1;

    public void RegisterItem(int index, ItemData item)
    {
        _quickSlots[index] = item;
        OnQuickSlotChanged?.Invoke(index, item);
    }

    private void UseSlot(int index)
    {
        ItemData item = _quickSlots[index];
        if (item == null) return;

        if (_currentSlotIndex == index)
        {
            _weaponController.UnequipWeapon();
            _currentSlotIndex = -1;
            OnSlotUsed?.Invoke(-1);
            return;
        }

        if (!_inventory.Items.Contains(item))
        {
            // 안전장치: 혹시라도 이벤트가 씹혔을 경우를 대비해 여기서도 지움
            RegisterItem(index, null);
            return;
        }

        if (item is WeaponData weaponData)
        {
            _weaponController.EquipWeapon(weaponData);
            _currentSlotIndex = index;
            OnSlotUsed?.Invoke(index);
        }
        else if (item is ConsumableData)
        {
            _inventory.RemoveItem(item);
            OnSlotUsed?.Invoke(index);
        }
    }
}