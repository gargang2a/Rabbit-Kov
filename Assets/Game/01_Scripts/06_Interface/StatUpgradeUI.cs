using UnityEngine;
using TMPro;

public class StatUpgradeUI : MonoBehaviour
{
    [Header("References")]
    public Player playerStats;
    public PlayerController playerCtrl;

    [Header("UI Text (숫자 표시용)")]
    public TMP_Text hpText;       // ★ 체력 수치 텍스트 (추가됨)
    public TMP_Text atkText;
    public TMP_Text staminaText;
    public TMP_Text speedText;

    [Header("Upgrade Settings (비용 및 증가량)")]
    public int upgradeCost = 100;            // ★ 강화 비용 (코인)

    public float hpIncreaseAmount = 20f;     // ★ 체력 증가량
    public int atkIncreaseAmount = 1;
    public float staminaIncreaseAmount = 10f;
    public float speedIncreaseAmount = 0.5f;

    void Start()
    {
        UpdateStatTexts();
    }

    // --- 버튼 연결 함수들 ---

    // ★ [추가] 체력 강화 버튼
    public void OnClickHpUp()
    {
        // 1. 코인을 쓸 수 있는지 확인 (UseCoin 함수가 true면 차감된 것)
        if (playerStats.UseCoin(upgradeCost))
        {
            // 2. 실제 스탯 업그레이드
            playerStats.UpgradeHp(hpIncreaseAmount);
            // 3. 텍스트 갱신
            UpdateStatTexts();
        }
    }

    public void OnClickAtkUp()
    {
        if (playerStats.UseCoin(upgradeCost))
        {
            playerStats.UpgradeAtk(atkIncreaseAmount);
            UpdateStatTexts();
        }
    }

    public void OnClickStaminaUp()
    {
        if (playerStats.UseCoin(upgradeCost))
        {
            playerStats.UpgradeStamina(staminaIncreaseAmount);
            UpdateStatTexts();
        }
    }

    public void OnClickSpeedUp()
    {
        if (playerStats.UseCoin(upgradeCost))
        {
            playerCtrl.UpgradeSpeed(speedIncreaseAmount);
            UpdateStatTexts();
        }
    }

    // --- 텍스트 갱신 ---
    public void UpdateStatTexts()
    {
        // 현재 스탯 수치를 UI에 표시
        if (hpText != null) hpText.text = playerStats.MaxHp.ToString("F0"); // 체력
        if (atkText != null) atkText.text = playerStats.Atk.ToString(); // 공격력
        if (staminaText != null) staminaText.text = playerStats.MaxStamina.ToString("F0"); // 스태미너
        if (speedText != null) speedText.text = playerCtrl.GetMoveSpeed().ToString("F1"); // 이속
    }
}