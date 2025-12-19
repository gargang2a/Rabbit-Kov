using UnityEngine;
using UnityEngine.UI;
using TMPro; // 텍스트 관리를 위해 필요합니다.

public class ItemSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _itemImage;    // 아이콘 이미지
    [SerializeField] private TMP_Text _itemName; // 아이템 이름 텍스트
    [SerializeField] private TMP_Text _itemPrice;// 아이템 가격 텍스트
    [SerializeField] private Button _buyButton;  // 구매 버튼

    private ItemData _itemData;

    // 슬롯에 데이터 채우기 (상점에서 아이템 목록을 만들 때 호출)
    public void SetItem(ItemData newItem)
    {
        if (newItem == null) return;

        _itemData = newItem;

        // 아이콘 설정 및 시각화
        if (_itemImage != null)
        {
            _itemImage.sprite = newItem.icon;
            _itemImage.enabled = true;

            // 알파값(투명도)을 1로 설정하여 보이게 함
            var color = _itemImage.color;
            color.a = 1f;
            _itemImage.color = color;
        }

        // 이름 및 가격 설정
        if (_itemName != null) _itemName.text = newItem.itemName;
        if (_itemPrice != null) _itemPrice.text = newItem.price.ToString() + " G";

        // 버튼 클릭 이벤트 연결 (기존 이벤트 제거 후 새로 연결)
        if (_buyButton != null)
        {
            _buyButton.onClick.RemoveAllListeners();
            _buyButton.onClick.AddListener(OnClickBuy);
        }
    }
    public void OnClickBuy()
    {
        if (_itemData != null)
        {
            // 씬에 있는 NPC_Interaction를 찾아 구매 함수 호출
            NPC_Interaction npc = Object.FindAnyObjectByType<NPC_Interaction>();
            if (npc != null)
            {
                npc.BuyItem(_itemData);
            }
        }
    }
}