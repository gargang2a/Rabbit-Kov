using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text _totalPriceText; // '살래' 왼쪽 가격 표시
    [SerializeField] private ItemSlot[] _uiSlots;

    [Header("Shop Settings")]
    [SerializeField] private List<ItemData> _shopItems;

    private List<ItemData> _selectedItems = new List<ItemData>();
    private int _totalPrice = 0;
    void Start()
    {
        InitializeShop();
    }
    public void InitializeShop()
    {
        for (int i = 0; i < _uiSlots.Length; i++)
        {
            if (i < _shopItems.Count)
            {
                _uiSlots[i].gameObject.SetActive(true);
                _uiSlots[i].SetItem(_shopItems[i]);
            }
            else _uiSlots[i].gameObject.SetActive(false);
        }
        ResetSelection();
    }
    public void UpdateTotalPrice(ItemData data, bool isSelected, int price)
    {
        if (isSelected)
        {
            if (!_selectedItems.Contains(data)) _selectedItems.Add(data);
            _totalPrice += price;
        }
        else
        {
            if (_selectedItems.Contains(data)) _selectedItems.Remove(data);
            _totalPrice -= price;
        }

        _totalPrice = Mathf.Max(0, _totalPrice);

        if (_totalPriceText != null)
        {
            _totalPriceText.text = _totalPrice.ToString("N0");
            Debug.Log($"[Shop] 현재 합계: {_totalPrice}원 (전달받은 가격: {price})");
        }
    }
    private void OnEnable()
    {
        ResetSelection();
    }
    public void ResetSelection()
    {
        _selectedItems.Clear();
        _totalPrice = 0;
        if (_totalPriceText != null) _totalPriceText.text = "0";
        foreach (var slot in _uiSlots) if (slot != null) slot.ResetSlot();
    }
    public void OnClickConfirmBuy()
    {
        if (_selectedItems.Count == 0) return;

        if (CoinManager.Instance != null && CoinManager.Instance.GetCurrentCoin() >= _totalPrice)
        {
            CoinManager.Instance.AddCoin(-_totalPrice);

            Inventory playerInv = FindObjectOfType<Inventory>();
            if (playerInv != null)
            {
                foreach (var item in _selectedItems) playerInv.AddItem(item);
                Debug.Log($"총 {_totalPrice}원 구매 완료!");
            }
            ResetSelection();
        }
        else
        {
            Debug.Log("코인이 부족합니다.");
        }
    }
    public void OnClickClose()
    {
        NPC_Interaction npc = FindObjectOfType<NPC_Interaction>();
        if (npc != null) npc.CloseAllNPCUI();
    }
}