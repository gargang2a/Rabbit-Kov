using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // ★ 마우스 호버링 감지용

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _btn;

    private ItemData _item; // 내부 데이터 (private)

    // ★ [핵심 수정] 외부에서 이 슬롯의 아이템을 읽을 수 있게 해주는 프로퍼티
    // 이 줄이 없어서 오류가 난 것입니다.
    public ItemData Item => _item;

    // 1. 슬롯에 아이템 채우기
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

    // 3. 버튼 클릭
    public void OnClickSlot()
    {
        if (_item != null && InventoryUI.Instance != null)
        {
            InventoryUI.Instance.OnItemClick(_item);
        }
    }

    // ==========================================
    // 마우스 호버링 감지 (퀵슬롯 등록용)
    // ==========================================
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 마우스가 들어오면 UI 매니저에게 알림
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.SetHoveredSlot(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스가 나가면 알림 해제
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.SetHoveredSlot(null);
        }
    }
}