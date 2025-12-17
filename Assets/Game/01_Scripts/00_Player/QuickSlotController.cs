using System;
using UnityEngine;

public class QuickSlotController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _slotCount = 4;

    [Header("References")]
    [SerializeField] private PlayerWeaponController _weaponController;
    [SerializeField] private Inventory _inventory;

    private ItemData[] _quickSlots;

    // ★ [추가] 현재 선택된 퀵슬롯 인덱스 (-1이면 없음)
    private int _currentSlotIndex = -1;

    public event Action<int, ItemData> OnQuickSlotChanged;
    public event Action<int> OnSlotUsed;

    private void Awake()
    {
        _quickSlots = new ItemData[_slotCount];
        if (_weaponController == null) _weaponController = GetComponent<PlayerWeaponController>();
        if (_inventory == null) _inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) HandleInput(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) HandleInput(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) HandleInput(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) HandleInput(3);
    }

    private void HandleInput(int index)
    {
        // 1. 등록 로직 (인벤토리 UI 호버링 중)
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
        OnQuickSlotChanged?.Invoke(index, item);
    }

    private void UseSlot(int index)
    {
        ItemData item = _quickSlots[index];

        // ★ [수정 1] 빈 슬롯이면 아무 반응 안 함 (애니메이션 X)
        if (item == null) return;

        // ★ [수정 2] 이미 선택된 슬롯을 다시 눌렀다면 -> 해제 (Toggle)
        if (_currentSlotIndex == index)
        {
            _weaponController.UnequipWeapon(); // 무기 해제
            _currentSlotIndex = -1;            // 선택 상태 초기화
            OnSlotUsed?.Invoke(-1);            // UI에게 "다 내려라(-1)" 신호 보냄
            return;
        }

        // ★ [수정 3] 인벤토리에 아이템이 있는지 확인
        if (!_inventory.Items.Contains(item))
        {
            Debug.Log("아이템이 인벤토리에 없습니다.");
            return;
        }

        // ★ [수정 4] 장착 로직
        if (item is WeaponData weaponData)
        {
            _weaponController.EquipWeapon(weaponData);
            _currentSlotIndex = index; // 현재 인덱스 갱신
            OnSlotUsed?.Invoke(index); // UI에게 "이거 올려라" 신호 보냄
        }
        else if (item is ConsumableData consumableData)
        {
            // 소모품은 장착 개념이 아니므로 인덱스 유지할지 말지 결정 필요
            // 여기서는 즉시 사용하고 슬롯 상태는 유지하지 않음
            _inventory.RemoveItem(item);
            // 소모품 사용 시에는 UI 애니메이션만 잠깐 보여줄 수도 있음
            OnSlotUsed?.Invoke(index);
            // 소모품은 사용 후 바로 선택 해제 상태로 돌리려면 아래 주석 해제
            // _currentSlotIndex = -1;
            // Invoke("DeselectLater", 0.5f); // 나중에 내려가게 하거나...
        }
    }
}