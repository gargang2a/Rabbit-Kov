using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    [Header("References")]
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private Player _player;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 시작할 때 연결 시도
        if (_player == null)
        {
            _player = FindObjectOfType<Player>();
            if (_player == null) Debug.LogWarning("🔴 [CoinManager] Player를 찾을 수 없습니다! Inspector에서 할당해주세요.");
        }

        UpdateCoinUI();
    }

    public int GetCurrentCoin()
    {
        // 없을 경우 다시 찾기 시도
        if (_player == null) _player = FindObjectOfType<Player>();

        if (_player == null) return 0;
        return _player.Coin;
    }

    public bool TrySpendCoin(int amount)
    {
        // 1. Player 참조 확인 (없으면 찾기)
        if (_player == null)
        {
            _player = FindObjectOfType<Player>();
        }

        // 2. 여전히 없으면 에러 처리
        if (_player == null)
        {
            Debug.LogError("🔴 [CoinManager] Player 참조가 없어 결제를 진행할 수 없습니다.");
            return false;
        }

        // 3. Player에게 결제 요청 (잔액 확인은 Player 내부에서 처리)
        bool success = _player.UseCoin(amount);

        if (success)
        {
            UpdateCoinUI();
        }

        return success;
    }

    public void AddCoin(int amount)
    {
        if (_player == null) _player = FindObjectOfType<Player>();

        if (_player != null)
        {
            _player.GainCoin(amount);
            UpdateCoinUI();
        }
    }

    public void UpdateCoinUI()
    {
        if (_player == null) return;

        if (_coinText != null)
        {
            _coinText.text = _player.Coin.ToString("N0");
        }
    }
}