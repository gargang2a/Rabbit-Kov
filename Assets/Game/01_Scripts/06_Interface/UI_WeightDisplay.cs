using UnityEngine;
using TMPro;

public class UI_WeightDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private TMP_Text _weightText;

    [Header("Settings")]
    [SerializeField] private Color _normalColor = Color.black;
    [SerializeField] private Color _heavyColor = Color.red;

    private void Update()
    {
        if (_player == null || _weightText == null) return;

        // 텍스트 표시 (예: 40.5 / 50)
        _weightText.text = $"{_player.CurrentWeight:F0} / {_player.MaxWeight:F0}kg";

        // ★ [수정됨] Player 스크립트한테 "지금 무거워?" 라고 물어봄
        // 이렇게 해야 Player에서 설정한 80% 기준을 똑같이 따릅니다.
        if (_player.IsOverweight)
        {
            _weightText.color = _heavyColor;
        }
        else
        {
            _weightText.color = _normalColor;
        }
    }
}