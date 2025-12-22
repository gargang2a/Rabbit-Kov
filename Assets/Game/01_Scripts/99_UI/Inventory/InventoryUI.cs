using UnityEngine;
using UnityEngine.UI; // ★ Text 컴포넌트 사용을 위해 필수

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("References")]
<<<<<<< HEAD
    [SerializeField] private Transform _slotsParent;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private PlayerWeaponController _weaponController;
=======
    [SerializeField] private Transform _slotsParent; // 슬롯들의 부모 (Grid)
    [SerializeField] private Inventory _inventory;   // 플레이어 인벤토리 데이터
    [SerializeField] private PlayerWeaponController _weaponController; // 무기 장착 컨트롤러

    [Header("UI Components")]
    [Tooltip("인벤토리 창 내부에 있는 무게 텍스트 (예: 10 / 50kg)")]
    [SerializeField] private Text _weightText; // ★ [New] 무게 텍스트 연결 변수
>>>>>>> 32eed657e6815770054abd4842b9273b24719489

    private ItemDropper _itemDropper;
    private InventorySlot[] _slots;
    private InventorySlot _hoveredSlot;
    public InventorySlot HoveredSlot => _hoveredSlot;

    private void Awake()
    {
        // 싱글톤 초기화
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
            // 무게 변경 이벤트도 구독
            _inventory.OnWeightChanged += UpdateWeightTextFromEvent;
        }

        UpdateUI();
    }
<<<<<<< HEAD
=======

    // 데이터 -> UI 갱신 (통합 관리)
>>>>>>> 32eed657e6815770054abd4842b9273b24719489
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

        // UI 갱신될 때 무게도 같이 갱신
        UpdateWeightText();
    }

    // ★ [핵심] Player.cs에서 호출하는 함수 (이게 없어서 에러가 났던 것)
    public void UpdateWeightText()
    {
        if (_inventory != null && _weightText != null)
        {
            // 소수점 1자리까지 표시 (예: 15.5 / 50 kg)
            _weightText.text = $"{_inventory.CurrentWeight:F1} / {_inventory.MaxWeight}kg";
        }
    }

    // 이벤트 연결용 (파라미터가 있는 버전)
    private void UpdateWeightTextFromEvent(float currentWeight)
    {
        UpdateWeightText();
    }
<<<<<<< HEAD
=======

    // ==========================================
    // 1. 좌클릭: 아이템 사용/장착
    // ==========================================
>>>>>>> 32eed657e6815770054abd4842b9273b24719489
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
<<<<<<< HEAD
=======

    // ==========================================
    // 2. 우클릭: 아이템 버리기
    // ==========================================
>>>>>>> 32eed657e6815770054abd4842b9273b24719489
    public void OnItemRightClick(InventorySlot slot, ItemData item)
    {
        if (item == null || _inventory == null) return;

        if (item.itemType == ItemType.Equipment && _weaponController != null)
        {
            if (_weaponController.CurrentWeapon != null && _weaponController.CurrentWeapon.BaseData == item)
            {
                _weaponController.UnequipWeapon();
            }
        }

        if (_inventory.RemoveItem(item))
        {
            ItemDropper dropper = _itemDropper != null ? _itemDropper : ItemDropper.Instance;

            if (dropper != null && item.worldPrefab != null)
            {
                dropper.DropItem(item.worldPrefab);
            }
        }
    }
<<<<<<< HEAD
=======

>>>>>>> 32eed657e6815770054abd4842b9273b24719489
    public void SetHoveredSlot(InventorySlot slot)
    {
        _hoveredSlot = slot;
    }
}