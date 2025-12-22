using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("References")]
    [SerializeField] private Transform _slotsParent;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private PlayerWeaponController _weaponController;

    private ItemDropper _itemDropper;
    private InventorySlot[] _slots;
    private InventorySlot _hoveredSlot;
    public InventorySlot HoveredSlot => _hoveredSlot;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

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
    public void OnItemClick(ItemData item)
    {
        if (item == null) return;

        if (item.itemType == ItemType.Equipment)
        {
            if (item is WeaponData weaponData && _weaponController != null)
                _weaponController.EquipWeapon(weaponData);
        }
        else if (item.itemType == ItemType.Consumable)
        {
            Debug.Log($"소모품 ({item.itemName}) 사용됨");
            if (_inventory != null) _inventory.RemoveItem(item);
        }
    }
    public void OnItemRightClick(InventorySlot slot, ItemData item)
    {
        if (item == null || _inventory == null) return;

        if (item.itemType == ItemType.Equipment && _weaponController != null)
        {
            if (_weaponController.CurrentWeapon != null && _weaponController.CurrentWeapon.BaseData == item)
            {
                _weaponController.UnequipWeapon();
                Debug.Log($"장착된 아이템 ({item.itemName}) 해제.");
            }
        }

        if (_inventory.RemoveItem(item))
        {
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
    public void SetHoveredSlot(InventorySlot slot)
    {
        _hoveredSlot = slot;
    }
}