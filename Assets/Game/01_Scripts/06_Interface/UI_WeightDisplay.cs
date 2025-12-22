using UnityEngine;
using TMPro;
using System;

public class UI_WeightDisplay : MonoBehaviour
{
    // ★ [1] 외부에서 접근하기 쉽게 싱글톤 추가
    public static UI_WeightDisplay Instance;

    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private TMP_Text _weightText;

    [Header("Settings")]
    [SerializeField] private Color _normalColor = Color.black;
    [SerializeField] private Color _heavyColor = Color.red;

    private float _currentWeightCache = 0f;

    private void Awake()
    {
        // ★ 싱글톤 초기화
        if (Instance == null) Instance = this;

        if (_player == null) _player = FindObjectOfType<Player>();
        if (_inventory == null) _inventory = FindObjectOfType<Inventory>();
    }

    private void Start()
    {
        if (_inventory != null)
        {
            _inventory.OnWeightChanged += UpdateWeightDisplay;
        }

        // 초기 표시
        ForceUpdate();
    }

    // ★ [2] 외부(가방 아이템)에서 강제로 UI를 갱신하게 만드는 함수
    public void ForceUpdate()
    {
        if (_player != null)
        {
            // 현재 플레이어의 무게를 가져와서 디스플레이 갱신 로직 실행
            UpdateWeightDisplay(_player.CurrentWeight);
        }
    }

    // 이벤트 콜백 함수
    private void UpdateWeightDisplay(float newTotalWeight)
    {
        if (_player == null || _weightText == null) return;

        _currentWeightCache = newTotalWeight;

        // 텍스트 표시 (현재무게 / 최대무게)
        // MaxWeight가 늘어났을 때 이 함수가 불리면 텍스트도 바뀝니다.
        _weightText.text = $"{_currentWeightCache:F0} / {_player.MaxWeight:F0}kg";

        // 색상 업데이트
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
        if (_inventory != null)
        {
            _inventory.OnWeightChanged -= UpdateWeightDisplay;
        }
    }
}