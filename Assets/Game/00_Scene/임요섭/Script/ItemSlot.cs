using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _iconImage;       // 아이템 아이콘
    [SerializeField] private TMP_Text _priceText;    // 가격 텍스트
    [SerializeField] private GameObject _selectedBorder; // 선택 표시 (테두리 등)

    // 내부 변수
    private ItemData _itemData;
    private bool _isSelected = false;
    private ShopManager _shopManager;
    private Button _myButton;

    private void Awake()
    {
        // 1. 내 오브젝트에 있는 버튼 컴포넌트 찾기
        _myButton = GetComponent<Button>();

        // 버튼이 없으면 자동으로 추가 (안전장치)
        if (_myButton == null)
        {
            _myButton = gameObject.AddComponent<Button>();
        }

        // 2. 클릭 이벤트 코드 연결
        _myButton.onClick.RemoveAllListeners(); // 중복 방지
        _myButton.onClick.AddListener(OnSlotClicked);

        // 3. 부모에 있는 ShopManager 찾기
        _shopManager = GetComponentInParent<ShopManager>();
    }

    // ShopManager가 데이터를 넣어주는 함수
    public void SetItem(ItemData data)
    {
        _itemData = data;
        _isSelected = false;

        if (_itemData != null)
        {
            gameObject.SetActive(true);

            // 데이터 연동 (아이콘, 가격)
            if (_iconImage != null) _iconImage.sprite = _itemData.icon;
            if (_priceText != null) _priceText.text = $"{_itemData.price}";
        }
        else
        {
            gameObject.SetActive(false);
        }

        UpdateSelectionVisual();
    }

    // ★ 핵심: 슬롯이 클릭되었을 때 실행되는 함수
    private void OnSlotClicked()
    {
        if (_itemData == null) return;

        // 1. 선택 상태 반전 (켜기/끄기)
        _isSelected = !_isSelected;

        // 2. 디버깅 로그 (클릭이 되는지 확인용)
        Debug.Log($"👆 [ItemSlot] 슬롯 클릭됨! 아이템: {_itemData.itemName}, 선택상태: {_isSelected}");

        // 3. 시각적 효과 갱신
        UpdateSelectionVisual();

        // 4. 매니저에게 알림 (가장 중요!)
        if (_shopManager != null)
        {
            _shopManager.UpdateTotalPrice(_itemData, _isSelected, _itemData.price);
        }
        else
        {
            Debug.LogError("🔴 [ItemSlot] ShopManager를 찾을 수 없습니다!");
        }
    }

    // 상점 초기화 시 호출
    public void ResetSlot()
    {
        _isSelected = false;
        UpdateSelectionVisual();
    }

    private void UpdateSelectionVisual()
    {
        // 선택 테두리가 있다면 켜고 끄기
        if (_selectedBorder != null)
            _selectedBorder.SetActive(_isSelected);

        // 아이콘 색상 변경으로 선택 표시 (테두리 없어도 티가 나게 함)
        if (_iconImage != null)
        {
            _iconImage.color = _isSelected ? new Color(0.5f, 1f, 0.5f) : Color.white; // 선택되면 초록빛
        }
    }
}