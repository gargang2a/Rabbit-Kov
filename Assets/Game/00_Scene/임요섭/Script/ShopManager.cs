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

    [Header("Audio Settings")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _sfxBuySuccess;
    [SerializeField] private AudioClip _sfxBuyFail;
    [SerializeField] private AudioClip _sfxConfirmButton;
    [SerializeField] private AudioClip _sfxCloseButton;

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

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
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

    // ★ 버튼에 연결된 함수 (살래 버튼)
    public void OnClickConfirmBuy()
    {
        PlaySFX(_sfxConfirmButton);

        Debug.Log($"🖱️ [Shop] 구매 버튼 클릭됨! (현재 선택된 아이템: {_selectedItems.Count}개, 총 가격: {_totalPrice})");

        // 아이템 미선택 시
        if (_selectedItems.Count == 0)
        {
            Debug.LogWarning("🟡 [Shop] 선택된 아이템이 없습니다.");
            PlaySFX(_sfxBuyFail);
            return;
        }

        if (CoinManager.Instance == null)
        {
            Debug.LogError("🔴 [Shop] CoinManager가 없습니다!");
            return;
        }

        if (_playerInventory == null) _playerInventory = FindObjectOfType<Inventory>();

        // 무게 체크
        float totalWeight = 0f;
        foreach (var i in _selectedItems)
        {
            totalWeight += i.weight;
        }

        if (_player.CurrentWeight + totalWeight <= _player.MaxWeight)
        {
            // 결제 시도
            bool purchaseSuccess = CoinManager.Instance.TrySpendCoin(_totalPrice);

            if (purchaseSuccess)
            {
                foreach (var item in _selectedItems)
                {
                    _playerInventory.AddItem(item);
                    Debug.Log($"🟢 [Shop] 구매 성공: {item.itemName}");
                }

                PlaySFX(_sfxBuySuccess);
                ResetSelection();

                // ★ [추가] 구매 성공 대사 출력
                if (NPC_Interaction.ActiveNPC != null)
                {
                    NPC_Interaction.ActiveNPC.ShowShopFeedback("거래해줘서 고마워");
                }
            }
            else
            {
                PlaySFX(_sfxBuyFail);
                int currentCoin = CoinManager.Instance.GetCurrentCoin();
                Debug.LogError($"🔴 [Shop] 돈 부족! (보유: {currentCoin}, 필요: {_totalPrice})");

                // ★ [추가] 구매 실패 대사 출력 (돈 부족)
                if (NPC_Interaction.ActiveNPC != null)
                {
                    NPC_Interaction.ActiveNPC.ShowShopFeedback("coin이나 무게가 모자라");
                }
            }
        }
        else
        {
            PlaySFX(_sfxBuyFail);
            Debug.Log("🔴 [Shop] 무게 초과");

            // ★ [추가] 구매 실패 대사 출력 (무게 초과)
            if (NPC_Interaction.ActiveNPC != null)
            {
                NPC_Interaction.ActiveNPC.ShowShopFeedback("coin이나 무게가 모자라");
            }
        }
    }

    // ★ 닫기 버튼 (말래 버튼)
    public void OnClickClose()
    {
        PlaySFX(_sfxCloseButton);

        // ★ [수정] 상점만 닫고 대화창을 띄움
        if (NPC_Interaction.ActiveNPC != null)
        {
            // 1. 상점 패널 끄기
            if (NPC_Interaction.ActiveNPC.ShopPanel != null)
            {
                NPC_Interaction.ActiveNPC.ShopPanel.SetActive(false);
            }

            // 2. 작별 대사 출력
            NPC_Interaction.ActiveNPC.ShowShopFeedback("벌써 가는거야?");
        }
        else
        {
            // 예외 처리: NPC를 못 찾으면 그냥 다 닫음
            NPC_Interaction npc = FindObjectOfType<NPC_Interaction>();
            if (npc != null) npc.CloseAllNPCUI();
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
}