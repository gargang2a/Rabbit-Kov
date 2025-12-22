// InventoryUI.cs 파일 전체 코드 (좌/우클릭, 드랍, 무게 시스템 연동 최종본)

using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance; // 싱글톤

    [Header("References")]
    [SerializeField] private Transform _slotsParent; // 슬롯들의 부모 (Grid)
    [SerializeField] private Inventory _inventory;   // 플레이어 인벤토리 데이터
    [SerializeField] private PlayerWeaponController _weaponController; // 무기 장착 컨트롤러
                                                                       // ItemDropper 필드는 싱글톤으로 대체

    private ItemDropper _itemDropper;
    private InventorySlot[] _slots;
    private InventorySlot _hoveredSlot;
    public InventorySlot HoveredSlot => _hoveredSlot;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // ItemDropper 참조 (싱글톤)
        _itemDropper = ItemDropper.Instance;

        if (_inventory == null)
        {
            _inventory = FindObjectOfType<Inventory>();
        }
    }

    private void Start()
    {
        _slots = _slotsParent.GetComponentsInChildren<InventorySlot>();

        if (_inventory != null)
        {
            _inventory.OnInventoryChanged += UpdateUI;
        }

        UpdateUI();
    }

    // 데이터 -> UI 갱신
    private void UpdateUI()
    {
        if (_inventory == null || _slots == null) return;

        var items = _inventory.Items;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (i < items.Count && items[i] != null)
                _slots[i].SetItem(items[i]);
            else
                _slots[i].ClearSlot();
        }
    }

    // ==========================================
    // 1. 좌클릭: 아이템 사용/장착 (InventorySlot에서 호출됨)
    // ==========================================
    public void OnItemClick(ItemData item) // ★ 이 메서드가 정의되어야 합니다.
    {
        if (item == null) return;

        if (item.itemType == ItemType.Equipment) // 무기 및 장비류
        {
            if (item is WeaponData weaponData && _weaponController != null)
                _weaponController.EquipWeapon(weaponData);
        }
        else if (item.itemType == ItemType.Consumable)
        {
            // 소모품 사용 로직
            Debug.Log($"소모품 ({item.itemName}) 사용됨");
            if (_inventory != null) _inventory.RemoveItem(item);
        }
    }

    // ==========================================
    // 2. 우클릭: 아이템 버리기 (InventorySlot에서 호출됨)
    // ==========================================
    public void OnItemRightClick(InventorySlot slot, ItemData item)
    {
        if (item == null || _inventory == null) return;

        // 1. 버리기 전 장착 해제 안전장치 (장비류인 경우)
        if (item.itemType == ItemType.Equipment && _weaponController != null)
        {
            if (_weaponController.CurrentWeapon != null && _weaponController.CurrentWeapon.BaseData == item)
            {
                _weaponController.UnequipWeapon();
                Debug.Log($"장착된 아이템 ({item.itemName}) 해제.");
            }
        }

        // 2. 인벤토리에서 아이템 데이터 제거 시도 (bool 반환 사용)
        if (_inventory.RemoveItem(item))
        {
            // 3. 제거 성공 시에만 월드에 아이템 드랍
            ItemDropper dropper = _itemDropper != null ? _itemDropper : ItemDropper.Instance;

            if (dropper != null)
            {
                if (item.worldPrefab != null)
                {
                    dropper.DropItem(item.worldPrefab);
                    Debug.Log($"아이템 ({item.itemName}) 월드에 드랍 완료.");
                }
                else
                {
                    Debug.LogError($"아이템 ({item.itemName}) 드랍 실패: worldPrefab이 ItemData에 설정되지 않았습니다.");
                }
            }
            else
            {
                Debug.LogWarning("ItemDropper 시스템이 없습니다. 아이템은 제거되었으나 월드에 드랍되지 않았습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"아이템 ({item.itemName}) 제거 실패: 인벤토리에 해당 아이템이 없거나 Inventory 컴포넌트에 문제가 있습니다.");
        }
    }

    // 3. 호버링 상태 갱신
    public void SetHoveredSlot(InventorySlot slot)
    {
        _hoveredSlot = slot;
    }
}