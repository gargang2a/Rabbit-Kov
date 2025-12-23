using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;
    
    [Header("UI References")]
    [SerializeField] private TMP_Text _totalPriceText;
    [SerializeField] private ItemSlot[] _uiSlots;

    [Header("Shop Settings")]
    [SerializeField] private List<ItemData> _shopItems;

    [SerializeField] private Player _player;

    private List<ItemData> _selectedItems = new List<ItemData>();
    private int _totalPrice = 0;
    private Inventory _playerInventory;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("⚠️ 씬에 ShopManager가 2개 이상입니다! 중복된 것을 삭제합니다.");
            Destroy(gameObject);
        }
    }
    void Start()
    {
        _playerInventory = FindObjectOfType<Inventory>();
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
            else
            {
                _uiSlots[i].gameObject.SetActive(false);
            }
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
        }
    }

    public void ResetSelection()
    {
        _selectedItems.Clear();
        _totalPrice = 0;

        if (_totalPriceText != null)
            _totalPriceText.text = "0";

        foreach (var slot in _uiSlots)
        {
            if (slot != null) slot.ResetSlot();
        }
    }

    // ★ 버튼에 연결된 함수
    public void OnClickConfirmBuy()
    {
        // 0. 버튼 클릭 확인 로그 (이게 안 뜨면 버튼 연결 문제)
        Debug.Log($"🖱️ [Shop] 구매 버튼 클릭됨! (현재 선택된 아이템: {_selectedItems.Count}개, 총 가격: {_totalPrice})");

        // 1. 아이템 선택 여부 확인
        if (_selectedItems.Count == 0)
        {
            Debug.LogWarning("🟡 [Shop] 선택된 아이템이 없습니다. (리스트가 비어있음)");
            return;
        }

        if (CoinManager.Instance == null)
        {
            Debug.LogError("🔴 [Shop] CoinManager가 씬에 없습니다! (싱글톤 인스턴스 null)");
            return;
        }

        if (_playerInventory == null)
        {
            _playerInventory = FindObjectOfType<Inventory>();
            if (_playerInventory == null)
            {
                Debug.LogError("🔴 [Shop] Inventory를 찾을 수 없습니다.");
                return;
            }
        }
        if (_playerInventory == null) _playerInventory = FindObjectOfType<Inventory>();
        bool purchaseSuccess = CoinManager.Instance.TrySpendCoin(_totalPrice);
        float totalWeight = 0f;
        foreach (var i in _selectedItems)
        {
            totalWeight += i.weight;
        }
        if(_player.CurrentWeight + totalWeight <= _player.MaxWeight)
        {
            if (purchaseSuccess)
            {
                foreach (var item in _selectedItems)
                {
                    _playerInventory.AddItem(item);
                    Debug.Log($"🟢 [Shop] 구매 성공 및 아이템 지급: {item.itemName}");
                }

                ResetSelection();
            }
            else
            {
                int currentCoin = CoinManager.Instance.GetCurrentCoin();
                Debug.LogError($"🔴 [Shop] 결제 실패! (보유 코인: {currentCoin}, 필요 코인: {_totalPrice}) - CoinManager나 Player 연결 상태를 확인하세요.");
            }
        }
        else
        {
            Debug.Log("무게 초과");
            CoinManager.Instance.AddCoin(_totalPrice);
        }
    }

    public void OnClickClose()
    {
        NPC_Interaction npc = FindObjectOfType<NPC_Interaction>();
        if (npc != null) npc.CloseAllNPCUI();
    }
}