using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 마우스 호버링 감지용

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _btn;

    private ItemData _item;

    // 퀵슬롯 컨트롤러가 이 슬롯의 아이템을 알기 위해 필요함
    public ItemData Item => _item;

    // 1. 슬롯 채우기
    public void SetItem(ItemData newItem)
    {
        _item = newItem;
        _iconImage.sprite = newItem.icon;

        var color = _iconImage.color;
        color.a = 1f;
        _iconImage.color = color;

        _iconImage.enabled = true;
    }

    // 2. 슬롯 비우기
    public void ClearSlot()
    {
        _item = null;
        _iconImage.sprite = null;

        var color = _iconImage.color;
        color.a = 0f;
        _iconImage.color = color;

        _iconImage.enabled = false;
    }

    // 3. 클릭 이벤트 (좌클릭 -> 장착/사용)
    // 인스펙터에서 Button 컴포넌트의 OnClick에 연결되어 있어야 함
    public void OnClickSlot()
    {
        if (_item != null && InventoryUI.Instance != null)
        {
            InventoryUI.Instance.OnItemClick(_item);
        }
    }

    // ==========================================
    // 마우스 호버링 (퀵슬롯 숫자키 등록용)
    // ==========================================
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.SetHoveredSlot(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.SetHoveredSlot(null);
        }
    }
}