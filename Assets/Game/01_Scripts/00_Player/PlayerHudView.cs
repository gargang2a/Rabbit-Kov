using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDView : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Player _player;

    [Header("Bars")]
    [SerializeField] private Image _hpBarImage;
    [SerializeField] private Image _staminaBarImage;
    [SerializeField] private Image _expBarImage;

    [Header("Texts")]
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _staminaText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _expText;
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _killText;

    // 활성화 되어있을때만 구독하기
    private void OnEnable()
    {
        _player.OnHpChanged += UpdateHp;
        _player.OnStaminaChanged += UpdateStamina;
        _player.OnExpChanged += UpdateExp;
        _player.OnCoinChanged += UpdateCoin;
        _player.OnKillChanged += UpdateKill;
    }

    // 비활성화 되면 구독 해제하기
    private void OnDisable()
    {
        _player.OnHpChanged -= UpdateHp;
        _player.OnStaminaChanged -= UpdateStamina;
        _player.OnExpChanged -= UpdateExp;
        _player.OnCoinChanged -= UpdateCoin;
        _player.OnKillChanged -= UpdateKill;
    }

    private void UpdateHp(float currentHp, float maxHp)
    {
            _hpBarImage.fillAmount = currentHp / maxHp;
            _hpText.text = $"{currentHp:F0} / {maxHp:F0}";
    }

    private void UpdateStamina(float currentStamina, float maxStamina)
    {
            _staminaBarImage.fillAmount = currentStamina / maxStamina;
            _staminaText.text = $"{currentStamina:F0} / {maxStamina:F0}";
    }

    private void UpdateExp(int level, int currentExp, int maxExp)
    {
            _expBarImage.fillAmount = (float)currentExp / maxExp;
            _levelText.text = $"Lv.{level}";
            _expText.text = $"{currentExp} / {maxExp}";
    }

    private void UpdateCoin(int coin)
    {
            _coinText.text = coin.ToString();
    }

    private void UpdateKill(int killCount)
    {
            _killText.text = killCount.ToString();
    }
}
