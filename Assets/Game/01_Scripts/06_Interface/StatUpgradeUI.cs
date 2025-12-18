using UnityEngine;
using TMPro;

public class StatUpgradeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _playerStats;       // 플레이어 스탯 스크립트
    [SerializeField] private PlayerController _playerCtrl; // 플레이어 이동 스크립트

    [Header("UI Text (숫자 표시용)")]
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _atkText;
    [SerializeField] private TMP_Text _staminaText;
    [SerializeField] private TMP_Text _speedText;

    [Header("Upgrade Settings")]
    [SerializeField] private int _upgradeCost = 100;

    [SerializeField] private float _hpIncreaseAmount = 20f;
    [SerializeField] private int _atkIncreaseAmount = 1;
    [SerializeField] private float _staminaIncreaseAmount = 10f;
    [SerializeField] private float _speedIncreaseAmount = 0.5f;

    private void OnEnable()
    {
        UpdateStatTexts();
    }

    private void Start()
    {
        UpdateStatTexts();
    }

    // --- 버튼 연결 함수들 ---
    public void OnClickHpUp()
    {
        if (_playerStats.UseCoin(_upgradeCost))
        {
            _playerStats.UpgradeHp(_hpIncreaseAmount);
            UpdateStatTexts();
        }
    }

    public void OnClickAtkUp()
    {
        if (_playerStats.UseCoin(_upgradeCost))
        {
            _playerStats.UpgradeAtk(_atkIncreaseAmount);
            UpdateStatTexts();
        }
    }

    public void OnClickStaminaUp()
    {
        if (_playerStats.UseCoin(_upgradeCost))
        {
            _playerStats.UpgradeStamina(_staminaIncreaseAmount);
            UpdateStatTexts();
        }
    }

    public void OnClickSpeedUp()
    {
        if (_playerStats.UseCoin(_upgradeCost))
        {
            _playerCtrl.UpgradeSpeed(_speedIncreaseAmount);
            UpdateStatTexts();
        }
    }

    // --- 텍스트 갱신 ---
    public void UpdateStatTexts()
    {
        if (_playerStats == null || _playerCtrl == null) return;

        if (_hpText != null) _hpText.text = _playerStats.MaxHp.ToString("F0");
        if (_atkText != null) _atkText.text = _playerStats.Atk.ToString();
        if (_staminaText != null) _staminaText.text = _playerStats.MaxStamina.ToString("F0");
        if (_speedText != null) _speedText.text = _playerCtrl.GetMoveSpeed().ToString("F1");
    }
}