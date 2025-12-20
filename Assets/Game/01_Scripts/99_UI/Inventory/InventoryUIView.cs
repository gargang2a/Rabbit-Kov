// InventoryManager.cs 파일 수정

using DG.Tweening;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("UI Reference")]
    [SerializeField] private RectTransform _inventoryPanel; // 이동할 인벤토리 패널

    [Header("Position Settings")]
    [Tooltip("닫혀있을 때 화면에 보일 너비 (책갈피 크기)")]
    [SerializeField, Range(-300f, 300f)] private float _visibleWidthClosed = 50f;

    [Tooltip("열렸을 때 우측 끝에서의 오프셋 (0=딱맞음, 양수=덜나옴, 음수=더나옴)")]
    [SerializeField] private float _openXOffset = 0f;

    [Header("Animation Settings")]
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InCubic;

    // ★ [수정] 초기 X, Y 위치를 저장하여 인스펙터 설정값을 존중.
    private float _initialXPosition;
    private float _initialYPosition;

    // ... (슬롯 관련 주석 생략)

    private List<InventorySlot> _slots = new List<InventorySlot>();

    private bool _isOpen = false;
    private Canvas _myCanvas;

    private void Start()
    {
        // ... (슬롯 생성 주석 생략)
    }

    private void Awake()
    {
        _myCanvas = GetComponentInParent<Canvas>();
        if (_inventoryPanel == null)
            _inventoryPanel = GetComponent<RectTransform>();

        // [핵심 수정] 현재 인스펙터에서 설정한 X, Y 위치를 캐싱합니다.
        // 이렇게 하면 인스펙터에서 설정한 초기 위치가 유지됩니다.
        _initialXPosition = _inventoryPanel.anchoredPosition.x;
        _initialYPosition = _inventoryPanel.anchoredPosition.y;

        // ★ [중요] 시작 시 레이아웃 강제 갱신 (해상도에 따른 정확한 너비 계산 보장)
        LayoutRebuilder.ForceRebuildLayoutImmediate(_inventoryPanel);

        // 초기화: 닫힌 상태
        _isOpen = false;

        // ★ [핵심 제거] Awake에서 위치를 강제로 닫힌 위치로 설정하는 코드 제거!
        // _inventoryPanel.anchoredPosition = new Vector2(CalculateClosedXPos(), _initialYPosition); 

        // 대신, 디자이너가 인스펙터에서 설정한 '닫힌 위치'를 그대로 유지하도록 합니다.
        // 디자이너는 반드시 인벤토리를 닫힌 위치(화면 밖)에 배치해야 합니다.

        // [안전장치] 만약 디자이너가 인벤토리를 열린 상태로 배치했다면, 닫힌 상태로 강제 이동합니다.
        // 이는 UI 시스템의 일관성을 위해 필요한 선택입니다.
        if (Mathf.Abs(_inventoryPanel.anchoredPosition.x - CalculateClosedXPos()) > 1f)
        {
            _inventoryPanel.anchoredPosition = new Vector2(CalculateClosedXPos(), _initialYPosition);
            Debug.Log("인벤토리 초기 위치가 열린 상태로 감지되어 닫힌 위치로 강제 이동했습니다.");
        }
        else
        {
            // 인스펙터 설정 위치가 닫힌 위치에 근접하면, Y축만 보존합니다.
            _inventoryPanel.anchoredPosition = new Vector2(_inventoryPanel.anchoredPosition.x, _initialYPosition);
        }

    }

    private void Update()
    {
        // 일시정지 중일 때는 인벤토리나 스탯창의 어떠한 입력도 받지 않음
        if (Time.timeScale == 0) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleStatus();
        }
    }

    // ... (이하 생략 - ToggleStatus, Open, Close 로직은 DOAnchorPosX를 사용하므로 Y축은 건드리지 않음)

    // --- Public Methods ---
    public void ToggleStatus()
    {
        if (_isOpen) Close();
        else Open();
    }

    private float CalculateOpenXPos()
    {
        return _openXOffset;
    }

    private float CalculateClosedXPos()
    {
        float currentWidth = _inventoryPanel.rect.width;
        return currentWidth - _visibleWidthClosed;
    }

    public void Open()
    {
        if (_isOpen || (UIManager.Instance != null && UIManager.Instance.IsAnyWindowOpen))
            return;

        _isOpen = true;

        if (UIManager.Instance != null)
            UIManager.Instance.SetWindowStatus(true);

        _inventoryPanel.DOKill();

        if (UIManager.Instance != null && _myCanvas != null)
        {
            UIManager.Instance.BringToFront(_myCanvas);
        }

        _inventoryPanel.SetAsLastSibling();

        float targetX = CalculateOpenXPos();

        _inventoryPanel.DOAnchorPosX(targetX, _slideDuration)
            .SetEase(_openEase)
            .SetUpdate(true);
    }

    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;

        if (UIManager.Instance != null)
            UIManager.Instance.SetWindowStatus(false);

        _inventoryPanel.DOKill();
        float targetX = CalculateClosedXPos();
        _inventoryPanel.DOAnchorPosX(targetX, _slideDuration)
            .SetEase(_closeEase)
            .SetUpdate(true);
    }
}