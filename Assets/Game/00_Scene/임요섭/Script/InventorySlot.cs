using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    private ItemData _currentItem;

    public bool IsEmpty => _currentItem == null;

    // 아이콘 이미지 컴포넌트가 할당되지 않았을 때를 대비한 안전장치
    private void Awake()
    {
        if (_iconImage == null)
        {
            // 자식 오브젝트 중 Image 컴포넌트를 가진 것을 찾음
            _iconImage = transform.Find("Icon")?.GetComponent<Image>();
        }
        ClearSlot();
    }
    public void AddItem(ItemData newItem)
    {
        _currentItem = newItem;
        _iconImage.sprite = newItem.icon; // ItemData에 있는 아이콘 적용
        _iconImage.enabled = true; // 이미지 컴포넌트 활성화
    }
    public void ClearSlot()
    {
        _currentItem = null;
        if (_iconImage != null)
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false; // 빈 칸일 때는 이미지를 숨깁니다.
        }
    }
}