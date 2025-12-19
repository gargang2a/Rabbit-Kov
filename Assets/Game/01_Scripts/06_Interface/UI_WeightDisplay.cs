// UI_WeightDisplay.cs 파일 전체 코드 (Update 제거 및 이벤트 구독)

using UnityEngine;
using TMPro;
using System; // OnDestroy에서 이벤트 구독 해제를 위해 필요

public class UI_WeightDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private Inventory _inventory; // ★ 인벤토리 레퍼런스 추가 (이벤트 구독용)
    [SerializeField] private TMP_Text _weightText;

    [Header("Settings")]
    [SerializeField] private Color _normalColor = Color.black;
    [SerializeField] private Color _heavyColor = Color.red;

    // 이 값은 이벤트 발생 시 업데이트되므로, Update() 루프는 돌지 않습니다.
    private float _currentWeightCache = 0f;

    private void Awake()
    {
        // 레퍼런스가 비어있으면 자동 찾기 시도
        if (_player == null)
            _player = FindObjectOfType<Player>();
        if (_inventory == null)
        {
            // Inventory가 Player에 붙어있을 가능성이 높으므로 GetComponentInParent 등을 활용하는 것도 좋음
            _inventory = FindObjectOfType<Inventory>();
        }
    }

    private void Start()
    {
        // ★★★ 1. 이벤트 구독
        if (_inventory != null)
        {
            _inventory.OnWeightChanged += UpdateWeightDisplay;
        }

        // 2. 초기 표시 (Player에서 현재 무게를 가져와 표시)
        if (_player != null)
        {
            // Player.CurrentWeight가 초기화되어 있다고 가정하고 표시합니다.
            UpdateWeightDisplay(_player.CurrentWeight);
        }

        // Update() 루프는 사용하지 않습니다. 이벤트 기반으로 작동합니다.
    }

    // ★★★ [콜백 함수] Inventory.OnWeightChanged 이벤트 발생 시 호출됨
    private void UpdateWeightDisplay(float newTotalWeight)
    {
        if (_player == null || _weightText == null) return;

        _currentWeightCache = newTotalWeight;

        // 텍스트 표시
        // Player의 MaxWeight를 가져옵니다.
        _weightText.text = $"{_currentWeightCache:F0} / {_player.MaxWeight:F0}kg";

        // 색상 업데이트 
        // Player의 IsOverweight가 CurrentWeight와 MaxWeight를 기반으로 계산된다고 가정합니다.
        if (_player.IsOverweight)
        {
            _weightText.color = _heavyColor;
        }
        else
        {
            _weightText.color = _normalColor;
        }
    }

    private void OnDestroy()
    {
        // ★★★ 이벤트 구독 해제 (메모리 누수 방지)
        if (_inventory != null)
        {
            _inventory.OnWeightChanged -= UpdateWeightDisplay;
        }
    }
}