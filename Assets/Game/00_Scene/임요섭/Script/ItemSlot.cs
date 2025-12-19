using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _itemImage;
    [SerializeField] private GameObject _selectVisual;

    [Header("Price Setting")]
    public int slotPrice;

    private ItemData _itemData;
    private bool _isChosen = false;
    public void SetItem(ItemData newItem)
    {
        if (newItem == null) return;
        _itemData = newItem;

        if (_itemImage != null)
        {
            _itemImage.sprite = newItem.icon;
            _itemImage.enabled = true;
        }
        if (_selectVisual != null) _selectVisual.SetActive(false);
    }
    public void OnToggleSelection()
    {
        if (_itemData == null) return;

        _isChosen = !_isChosen;
        if (_selectVisual != null) _selectVisual.SetActive(_isChosen);

        ShopManager shop = FindObjectOfType<ShopManager>();
        if (shop != null)
        {
            shop.UpdateTotalPrice(_itemData, _isChosen, slotPrice);
        }
    }
    public void ResetSlot()
    {
        _isChosen = false;
        if (_selectVisual != null) _selectVisual.SetActive(false);
    }
}