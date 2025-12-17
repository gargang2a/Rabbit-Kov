using System;
using UnityEngine;

public class QuickSlotController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _slotCount = 4;

    [Header("References")]
    [SerializeField] private PlayerWeaponController _weaponController;
    [SerializeField] private Inventory _inventory;

    // 퀵슬롯 데이터
    private ItemData[] _quickSlots;

    // ★ UI 갱신을 위한 이벤트 2개
    // 1. 슬롯의 내용물이 바뀌었을 때 (아이콘 변경용)
    public event Action<int, ItemData> OnQuickSlotChanged;
    // 2. 슬롯이 선택(사용)되었을 때 (애니메이션용)
    public event Action<int> OnSlotUsed;

    private void Awake()
    {
        _quickSlots = new ItemData[_slotCount];
        if (_weaponController == null) _weaponController = GetComponent<PlayerWeaponController>();
        if (_inventory == null) _inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
        // 입력 처리는 여기서만 담당합니다.
        if (Input.GetKeyDown(KeyCode.Alpha1)) HandleInput(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) HandleInput(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) HandleInput(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) HandleInput(3);
    }

    private void HandleInput(int index)
    {
        // 1. 등록 로직 (인벤토리 UI가 열려있고 호버링 중일 때)
        if (InventoryUI.Instance != null && InventoryUI.Instance.HoveredSlot != null)
        {
            ItemData itemToRegister = InventoryUI.Instance.HoveredSlot.Item;
            if (itemToRegister != null)
            {
                RegisterItem(index, itemToRegister);
            }
            return;
        }

        // 2. 사용 로직
        UseSlot(index);
    }

    public void RegisterItem(int index, ItemData item)
    {
        _quickSlots[index] = item;
        Debug.Log($"퀵슬롯 {index + 1}번에 {item.itemName} 등록됨");

        // UI에게 아이콘 바꾸라고 알림
        OnQuickSlotChanged?.Invoke(index, item);
    }

    private void UseSlot(int index)
    {
        // UI에게 애니메이션 재생하라고 알림 (아이템 유무 상관없이 반응)
        OnSlotUsed?.Invoke(index);

        ItemData item = _quickSlots[index];
        if (item == null) return;

        if (!_inventory.Items.Contains(item))
        {
            Debug.Log("아이템이 인벤토리에 없습니다.");
            // 필요 시 자동 삭제: RegisterItem(index, null);
            return;
        }

        if (item is WeaponData weaponData)
        {
            _weaponController.EquipWeapon(weaponData);
        }
        else if (item is ConsumableData consumableData)
        {
            Debug.Log($"소모품 사용: {item.itemName}");
            _inventory.RemoveItem(item);
            // 소모품 사용 후 퀵슬롯 유지 여부는 기획에 따라 결정
        }
    }
}