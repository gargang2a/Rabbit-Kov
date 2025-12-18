using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;
    public TextMeshProUGUI coinText; // 인스펙터에서 Coin Text 연결
    private int currentCoin = 9999; // 초기값

    void Awake() => Instance = this;

    public void AddCoin(int amount)
    {
        currentCoin += amount;
        UpdateCoinUI();
    }
    public void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = currentCoin.ToString();
    }
}