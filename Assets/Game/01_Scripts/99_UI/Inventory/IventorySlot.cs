// InventorySlot.cs 파일 수정 (좌클릭 로직 재교정)

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// IPointerClickHandler 추가
public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _btn;

    private ItemData _item;

    public ItemData Item => _item;
    public bool IsEmpty => _item == null;

    // 1. 슬롯 채우기 (생략)
    public void SetItem(ItemData newItem)
    {
        _item = newItem;
        _iconImage.sprite = newItem.icon;

        var color = _iconImage.color;
        color.a = 1f;
        _iconImage.color = color;

        _iconImage.enabled = true;
    }

    // 2. 슬롯 비우기 (생략)
    public void ClearSlot()
    {
        _item = null;
        _iconImage.sprite = null;

        var color = _iconImage.color;
        color.a = 0f;
        _iconImage.color = color;

        _iconImage.enabled = false;
    }

    // 3. 클릭 이벤트 (좌/우클릭 로직 통합) - [핵심 재수정]
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_item == null || InventoryUI.Instance == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // ★★★ [수정됨] 좌클릭은 OnItemClick(사용/장착)을 호출해야 합니다.
            InventoryUI.Instance.OnItemClick(_item);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 우클릭: 버리기
            InventoryUI.Instance.OnItemRightClick(this, _item);
        }
    }

    // 4. 마우스 호버링 (생략)
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