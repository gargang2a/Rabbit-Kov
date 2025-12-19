using UnityEngine;
using TMPro; // TextMeshPro 필수
using UnityEngine.UI;

public class UI_StatusWindow : MonoBehaviour
{
    [Header("Target References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerController _controller; // 이동속도 가져오기용

    [Header("UI Texts (숫자 표시)")]
    [SerializeField] private TMP_Text _atkText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _spText;      // Stamina
    [SerializeField] private TMP_Text _spdText;     // Speed
    [SerializeField] private TMP_Text _pointText;   // 남은 포인트 (Point: 999)

    [Header("Upgrade Buttons (+)")]
    [SerializeField] private Button _btnAtk;
    [SerializeField] private Button _btnHp;
    [SerializeField] private Button _btnSp;
    [SerializeField] private Button _btnSpd;

    private void Start()
    {
        // 버튼 클릭 이벤트 연결
        if (_btnAtk) _btnAtk.onClick.AddListener(() => _player.TryUpgradeAtk());
        if (_btnHp) _btnHp.onClick.AddListener(() => _player.TryUpgradeHp());
        if (_btnSp) _btnSp.onClick.AddListener(() => _player.TryUpgradeStamina());
        if (_btnSpd) _btnSpd.onClick.AddListener(() => _player.TryUpgradeSpeed());
    }

    private void Update()
    {
        if (_player == null) return;

        // 1. 텍스트 실시간 갱신
        if (_pointText) _pointText.text = $"Point : {_player.StatPoint}";
        if (_atkText) _atkText.text = $"{_player.Atk}";
        if (_hpText) _hpText.text = $"{_player.MaxHp:F0}";
        if (_spText) _spText.text = $"{_player.MaxStamina:F0}";

        if (_controller != null && _spdText != null)
        {
            _spdText.text = $"{_controller.GetMoveSpeed():F1}";
        }

        // 2. 포인트가 없으면 + 버튼 비활성화 (선택 사항)
        // 포인트가 있으면 버튼이 켜지고, 없으면 꺼지게 만듦
        bool hasPoint = _player.StatPoint > 0;

        if (_btnAtk) _btnAtk.interactable = hasPoint;
        if (_btnHp) _btnHp.interactable = hasPoint;
        if (_btnSp) _btnSp.interactable = hasPoint;
        if (_btnSpd) _btnSpd.interactable = hasPoint;
    }
}