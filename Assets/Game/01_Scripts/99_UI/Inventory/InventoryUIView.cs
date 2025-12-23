using DG.Tweening;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("UI Reference")]
    [SerializeField] private RectTransform _inventoryPanel;

    [Header("Position Settings")]
    [Tooltip("닫혀있을 때 화면에 보일 너비 (책갈피 크기)")]
    [SerializeField, Range(-300f, 300f)] private float _visibleWidthClosed = 50f;

    [Tooltip("열렸을 때 우측 끝에서의 오프셋 (0=딱맞음, 양수=덜나옴, 음수=더나옴)")]
    [SerializeField] private float _openXOffset = 0f;

    [Header("Animation Settings")]
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InCubic;

    private float _initialXPosition;
    private float _initialYPosition;

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

        _initialXPosition = _inventoryPanel.anchoredPosition.x;
        _initialYPosition = _inventoryPanel.anchoredPosition.y;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_inventoryPanel);

        _isOpen = false;

        if (Mathf.Abs(_inventoryPanel.anchoredPosition.x - CalculateClosedXPos()) > 1f)
        {
            _inventoryPanel.anchoredPosition = new Vector2(CalculateClosedXPos(), _initialYPosition);
            Debug.Log("인벤토리 초기 위치가 열린 상태로 감지되어 닫힌 위치로 강제 이동했습니다.");
        }
        else
        {
            _inventoryPanel.anchoredPosition = new Vector2(_inventoryPanel.anchoredPosition.x, _initialYPosition);
        }

    }

    private void Update()
    {
        if (Time.timeScale == 0 && !_isOpen) return;
        NPC_Interaction npc = FindObjectOfType<NPC_Interaction>();
        if (npc != null && npc.IsDialogueActive()) return;
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleStatus();
        }
    }
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