using UnityEngine;
using DG.Tweening;

public class InventoryUIView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform _inventoryRect;

    [Header("Position Settings")]
    [Tooltip("닫혀있을 때 화면에 보일 너비 (책갈피 크기)")]
    [SerializeField, Range(0f, 200f)] private float _visibleWidthClosed = 50f;

    [Tooltip("열렸을 때 우측 끝에서의 오프셋 (0=딱맞음, 양수=덜나옴, 음수=더나옴)")]
    [SerializeField] private float _openXOffset = 0f;

    [Header("Editor Preview")]
    [Tooltip("체크하면 에디터에서 열린 위치를 미리볼 수 있습니다.")]
    [SerializeField] private bool _previewOpenState = false;

    [Header("Animation Settings")]
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InCubic;

    // 내부 상태
    private bool _isOpen = false;
    private float _panelWidth;

    private void Awake()
    {
        if (_inventoryRect == null)
            _inventoryRect = GetComponent<RectTransform>();

        UpdatePanelWidth();

        // 게임 시작 시 닫힌 상태로 초기화
        _isOpen = false;
        _inventoryRect.anchoredPosition = new Vector2(GetClosedXPos(), 0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    /// <summary>
    /// Inspector 값을 변경할 때마다 호출 (실시간 미리보기)
    /// </summary>
    private void OnValidate()
    {
        if (_inventoryRect != null)
        {
            UpdatePanelWidth();

            // Preview 체크박스에 따라 위치를 즉시 이동
            float targetX = _previewOpenState ? GetOpenXPos() : GetClosedXPos();
            _inventoryRect.anchoredPosition = new Vector2(targetX, 0);
        }
    }

    public void ToggleInventory()
    {
        if (_isOpen) Close();
        else Open();
    }

    private void Open()
    {
        _isOpen = true;
        _inventoryRect.DOKill();

        _inventoryRect.DOAnchorPosX(GetOpenXPos(), _slideDuration)
            .SetEase(_openEase)
            .SetUpdate(true);
    }

    private void Close()
    {
        _isOpen = false;
        _inventoryRect.DOKill();

        _inventoryRect.DOAnchorPosX(GetClosedXPos(), _slideDuration)
            .SetEase(_closeEase)
            .SetUpdate(true);
    }

    // --- 계산 로직 ---

    private void UpdatePanelWidth()
    {
        // RectTransform의 너비가 변경되었을 수 있으므로 갱신
        _panelWidth = _inventoryRect.rect.width;
    }

    private float GetClosedXPos()
    {
        // 전체 너비에서 '보여질 만큼'을 뺀 위치 (화면 밖으로 나감)
        return _panelWidth - _visibleWidthClosed;
    }

    private float GetOpenXPos()
    {
        // 0이면 화면 끝에 딱 붙음.
        // 값을 조절하여 덜 나오게 하거나 더 나오게 함.
        return _openXOffset;
    }
}