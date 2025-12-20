using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;
    public TextMeshProUGUI coinText;
    private int currentCoin = 9999;

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
    public int GetCurrentCoin()
    {
        return currentCoin;
    }
}