using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _iconImage;  // 아이템 아이콘이 표시될 자식 이미지
    [SerializeField] private Button _btn;       // 클릭 처리를 위한 버튼

    private ItemData _item; // 현재 이 슬롯에 담긴 아이템 데이터

    // 1. 슬롯에 아이템 채우기
    public void SetItem(ItemData newItem)
    {
        _item = newItem;
        _iconImage.sprite = newItem.icon;

        // 색상을 흰색(불투명)으로 변경
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

        // 색상을 투명하게 변경 (혹은 enabled = false)
        var color = _iconImage.color;
        color.a = 0f;
        _iconImage.color = color;

        _iconImage.enabled = false;
    }

    // 3. 버튼 클릭 시 호출될 함수
    public void OnClickSlot()
    {
        if (_item != null)
        {
            // UI 매니저에게 "나 클릭됐어!"라고 알림
            InventoryUI.Instance.OnItemClick(_item);
        }
    }
}