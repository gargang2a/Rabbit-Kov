using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance; // 싱글톤

    [Header("References")]
    [SerializeField] private Transform _slotsParent; // 슬롯들의 부모 (Grid)
    [SerializeField] private Inventory _inventory;   // 플레이어 인벤토리 데이터
    [SerializeField] private PlayerWeaponController _weaponController; // 무기 장착 컨트롤러

    private InventorySlot[] _slots; // 슬롯 배열

    // 현재 마우스가 올라가 있는 슬롯 (숫자키 등록용)
    private InventorySlot _hoveredSlot;
    public InventorySlot HoveredSlot => _hoveredSlot;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // 1. 슬롯 컴포넌트들 가져오기
        _slots = _slotsParent.GetComponentsInChildren<InventorySlot>();

        // 2. 인벤토리 데이터 변경 구독
        if (_inventory != null)
        {
            _inventory.OnInventoryChanged += UpdateUI;
        }

        // 3. 초기화
        UpdateUI();
    }

    // 데이터 -> UI 갱신
    private void UpdateUI()
    {
        var items = _inventory.Items;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (i < items.Count)
                _slots[i].SetItem(items[i]);
            else
                _slots[i].ClearSlot();
        }
    }

    // ==========================================
    // 상호작용 로직
    // ==========================================

    // 1. 좌클릭: 아이템 사용/장착
    public void OnItemClick(ItemData item)
    {
        if (item == null) return;

        Debug.Log($"아이템 좌클릭: {item.itemName}");

        if (item is WeaponData weaponData)
        {
            if (_weaponController != null)
                _weaponController.EquipWeapon(weaponData);
        }
        else if (item is ConsumableData consumableData)
        {
            Debug.Log("소모품 사용됨");
            _inventory.RemoveItem(item);
        }
    }

    // 2. 호버링 상태 갱신 (퀵슬롯 컨트롤러가 가져다 씀)
    public void SetHoveredSlot(InventorySlot slot)
    {
        _hoveredSlot = slot;
    }
}