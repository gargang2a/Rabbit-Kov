using UnityEngine;
using UnityEngine.UI; // LayoutRebuilder 사용을 위해 추가
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

    [Header("Animation Settings")]
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InCubic;

    // 내부 상태
    private bool _isOpen = false;

    // ★ [수정] _panelWidth 변수 제거: 실시간 계산으로 변경

    private void Awake()
    {
        if (_inventoryRect == null)
            _inventoryRect = GetComponent<RectTransform>();

        // ★ [중요] 시작 시 레이아웃 강제 갱신 (해상도에 따른 정확한 너비 계산 보장)
        LayoutRebuilder.ForceRebuildLayoutImmediate(_inventoryRect);

        // 초기화: 닫힌 상태
        _isOpen = false;
        _inventoryRect.anchoredPosition = new Vector2(CalculateClosedXPos(), 0);
    }

    private void Update()
    {
        // ★ [Tip] 실제 게임에서는 Input Manager나 Event System을 사용하는 것을 권장
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    // --- Public Methods ---

    public void ToggleInventory()
    {
        if (_isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (_isOpen) return; // 이미 열려있으면 무시

        _isOpen = true;
        _inventoryRect.DOKill(); // 기존 애니메이션 중단

        // ★ [최적화] 열릴 때마다 목표 위치 재계산 (해상도 변경 대응)
        float targetX = CalculateOpenXPos();

        _inventoryRect.DOAnchorPosX(targetX, _slideDuration)
            .SetEase(_openEase)
            .SetUpdate(true); // TimeScale이 0이어도(일시정지) UI는 작동하도록 설정
    }

    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        _inventoryRect.DOKill();

        // ★ [최적화] 닫힐 때마다 목표 위치 재계산
        float targetX = CalculateClosedXPos();

        _inventoryRect.DOAnchorPosX(targetX, _slideDuration)
            .SetEase(_closeEase)
            .SetUpdate(true);
    }

    // --- Calculation Logic ---

    /// <summary>
    /// 현재 RectTransform의 너비를 기반으로 닫힌 위치(X)를 계산합니다.
    /// </summary>
    private float CalculateClosedXPos()
    {
        // ★ [핵심] 변수에 저장된 값이 아닌, 현재 프레임의 실제 너비를 사용
        float currentWidth = _inventoryRect.rect.width;

        // Pivot이 (1, 0.5) 즉 우측 기준일 때:
        // X가 0이면 화면 우측 끝.
        // X가 Width이면 화면 밖으로 완전히 나감.
        // 따라서 (Width - 보이는 양) 만큼 이동하면 '보이는 양'만 남고 나감.
        return currentWidth - _visibleWidthClosed;
    }

    private float CalculateOpenXPos()
    {
        // Pivot이 (1, 0.5)일 때 0이면 딱 맞게 열림
        return _openXOffset;
    }

    // --- Editor Preview (Optional) ---
#if UNITY_EDITOR
    [Header("Editor Debug")]
    [SerializeField] private bool _previewOpenState = false;

    private void OnValidate()
    {
        if (_inventoryRect == null) return;

        // 에디터에서도 즉시 반영되도록 계산
        float targetX = _previewOpenState ? CalculateOpenXPos() : CalculateClosedXPos();
        _inventoryRect.anchoredPosition = new Vector2(targetX, 0);
    }
#endif
}