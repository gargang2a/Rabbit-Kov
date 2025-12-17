using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class StatusUIView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform _inventoryRect;

    [Header("Position Settings")]
    [Tooltip("닫혀있을 때 화면에 보일 너비 (책갈피 크기)")]
    [SerializeField, Range(0f, 200f)] private float _visibleWidthClosed = 70.7f;

    [Tooltip("열렸을 때 우측 끝에서의 오프셋 (0=딱맞음, 양수=덜나옴, 음수=더나옴)")]
    [SerializeField] private float _openXOffset = 1500f;

    [Header("Animation Settings")]
    [SerializeField] private float _slideDuration = 0.4f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InCubic;

    // 내부 상태
    private bool _isOpen = false;
    private Canvas _myCanvas;

    private void Awake()
    {
        _myCanvas = GetComponentInParent<Canvas>();
        if (_inventoryRect == null)
            _inventoryRect = GetComponent<RectTransform>();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.BringToFront(_myCanvas);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_inventoryRect);

        _isOpen = false;
        _inventoryRect.anchoredPosition = new Vector2(CalculateClosedXPos(), 0);
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

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

    public void Open()
    {
        if (_isOpen || (UIManager.Instance != null && UIManager.Instance.IsAnyWindowOpen))
            return;

        _isOpen = true;

        if (UIManager.Instance != null)
            UIManager.Instance.SetWindowStatus(true);

        _inventoryRect.DOKill();

        if (UIManager.Instance != null && _myCanvas != null)
        {
            UIManager.Instance.BringToFront(_myCanvas);
        }

        float targetX = CalculateOpenXPos();
        _inventoryRect.DOAnchorPosX(targetX, _slideDuration)
            .SetEase(_openEase)
            .SetUpdate(true);
    }

    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;

        if (UIManager.Instance != null)
            UIManager.Instance.SetWindowStatus(false);

        // ★ [추가됨] 창이 닫힐 때 강제로 크로스헤어 모드로 복귀 (안전장치)
        if (DynamicCrosshair.Instance != null)
        {
            DynamicCrosshair.Instance.SetUIHoverState(false);
        }

        _inventoryRect.DOKill();
        float targetX = CalculateClosedXPos();
        _inventoryRect.DOAnchorPosX(targetX, _slideDuration)
            .SetEase(_closeEase)
            .SetUpdate(true);
    }

    private float CalculateClosedXPos()
    {
        float currentWidth = _inventoryRect.rect.width;
        return currentWidth - _visibleWidthClosed;
    }

    private float CalculateOpenXPos()
    {
        return _openXOffset;
    }

#if UNITY_EDITOR
    [Header("Editor Debug")]
    [SerializeField] private bool _previewOpenState = false;

    private void OnValidate()
    {
        if (_inventoryRect == null) return;
        float targetX = _previewOpenState ? CalculateOpenXPos() : CalculateClosedXPos();
        _inventoryRect.anchoredPosition = new Vector2(targetX, 0);
    }
#endif
}