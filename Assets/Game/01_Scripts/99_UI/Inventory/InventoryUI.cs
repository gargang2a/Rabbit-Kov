using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance; // 싱글톤 (슬롯들이 접근하기 위해)

    [Header("References")]
    [SerializeField] private Transform _slotsParent; // 슬롯 20개가 모여있는 부모 오브젝트 (Grid)
    [SerializeField] private Inventory _inventory;   // 플레이어의 인벤토리 데이터
    [SerializeField] private PlayerWeaponController _weaponController; // 무기 장착용 컨트롤러

    private InventorySlot[] _slots; // 자식 슬롯들 배열

    private InventorySlot _hoveredSlot;
    public InventorySlot HoveredSlot => _hoveredSlot;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // 1. 부모 아래에 있는 모든 슬롯 컴포넌트를 찾아옴
        _slots = _slotsParent.GetComponentsInChildren<InventorySlot>();

        // 2. 인벤토리 데이터 변경 이벤트 구독 (데이터가 변하면 UpdateUI 자동 실행)
        if (_inventory != null)
        {
            _inventory.OnInventoryChanged += UpdateUI;
        }

        // 3. 초기 화면 갱신
        UpdateUI();
    }

    public void SetHoveredSlot(InventorySlot slot)
    {
        _hoveredSlot = slot;
    }

    // 인벤토리 데이터 -> UI 동기화
    private void UpdateUI()
    {
        // 인벤토리 리스트 가져오기
        var items = _inventory.Items;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (i < items.Count)
            {
                // 데이터가 있으면 슬롯에 채움
                _slots[i].SetItem(items[i]);
            }
            else
            {
                // 데이터가 없으면 슬롯 비움
                _slots[i].ClearSlot();
            }
        }
    }

    // ★ 슬롯이 클릭되었을 때 실행되는 로직 (사용/장착)
    public void OnItemClick(ItemData item)
    {
        if (item == null) return;

        Debug.Log($"아이템 클릭됨: {item.itemName}");

        // 1. 무기라면 -> 장착
        if (item is WeaponData weaponData)
        {
            if (_weaponController != null)
            {
                _weaponController.EquipWeapon(weaponData);
                Debug.Log("무기 장착 완료");
            }
        }
        // 2. 소모품이라면 -> 사용 (체력 회복 등)
        else if (item is ConsumableData consumableData)
        {
            // 예: _inventory.GetComponent<Player>().Heal(consumableData.healAmount);
            Debug.Log("소모품 사용 (구현 필요)");

            // 소모품은 사용 후 사라짐
            _inventory.RemoveItem(item);
        }
    }
}