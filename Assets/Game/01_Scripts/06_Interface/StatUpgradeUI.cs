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
    [SerializeField] private TMP_Text _pointText; // ★ [추가] 남은 포인트 표시용 텍스트

    // [삭제] 기존의 Cost, IncreaseAmount 변수들은 이제 Player 스크립트 내부에서 처리하므로 필요 없습니다.

    private void OnEnable()
    {
        UpdateStatTexts();
    }

    private void Start()
    {
        UpdateStatTexts();
    }

    private void Update()
    {
        // 실시간으로 갱신 (레벨업 직후 바로 반영되도록)
        UpdateStatTexts();
    }

    // --- 버튼 연결 함수들 (기존 이름 유지) ---

    public void OnClickHpUp()
    {
        // 코인(UseCoin) 대신 스텟 포인트(TryUpgradeHp) 사용
        if (_playerStats.TryUpgradeHp())
        {
            UpdateStatTexts(); // 성공하면 UI 갱신
        }
        else
        {
            Debug.Log("포인트가 부족합니다.");
        }
    }

    public void OnClickAtkUp()
    {
        if (_playerStats.TryUpgradeAtk())
        {
            UpdateStatTexts();
        }
        else
        {
            Debug.Log("포인트가 부족합니다.");
        }
    }

    public void OnClickStaminaUp()
    {
        if (_playerStats.TryUpgradeStamina())
        {
            UpdateStatTexts();
        }
        else
        {
            Debug.Log("포인트가 부족합니다.");
        }
    }

    public void OnClickSpeedUp()
    {
        // Player 스크립트에 만들어둔 TryUpgradeSpeed 함수 호출
        if (_playerStats.TryUpgradeSpeed())
        {
            UpdateStatTexts();
        }
        else
        {
            Debug.Log("포인트가 부족합니다.");
        }
    }

    // --- 텍스트 갱신 ---
    public void UpdateStatTexts()
    {
        if (_playerStats == null || _playerCtrl == null) return;

        // 1. 남은 포인트 표시 (새로 추가됨)
        if (_pointText != null)
            _pointText.text = $"{_playerStats.StatPoint}";

        // 2. 스텟 수치 표시
        if (_hpText != null) _hpText.text = _playerStats.MaxHp.ToString("F0");
        if (_atkText != null) _atkText.text = _playerStats.Atk.ToString();
        if (_staminaText != null) _staminaText.text = _playerStats.MaxStamina.ToString("F0");
        if (_speedText != null) _speedText.text = _playerCtrl.GetMoveSpeed().ToString("F1");
    }
}