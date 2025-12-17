using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WeaponHUD : MonoBehaviour
{
    // ---------------------------------------------------------
    // 멤버 변수
    // ---------------------------------------------------------
    [Header("1. References")]
    [SerializeField] private QuickSlotController _controller; // ★ 연결 필요
    [SerializeField] private RectTransform[] _slotRects;
    [SerializeField] private Image[] _slotImages;

    // ★ 기존 WeaponUIData는 삭제 (ItemData로 대체됨)

    [Header("3. Position Settings")]
    [SerializeField] private float _selectedY = 30f;
    [SerializeField] private float _defaultY = 0f;

    [Header("4. Rotation Settings (Z-Axis)")]
    [SerializeField] private float _selectedRotation = 0f;
    [SerializeField] private float _defaultRotation = -15f;

    [Header("5. Alpha Settings (0~1)")]
    [Tooltip("255 = 1.0")]
    [SerializeField, Range(0f, 1f)] private float _selectedAlpha = 1f;

    [Tooltip("225 = ~0.88")]
    [SerializeField, Range(0f, 1f)] private float _defaultAlpha = 0.882f;

    [Header("6. Animation Settings")]
    [SerializeField] private float _animDuration = 0.2f;
    [SerializeField] private Ease _animEase = Ease.OutBack;

    // 내부 상태
    private int _currentIndex = -1;

    // ---------------------------------------------------------
    // Unity Events
    // ---------------------------------------------------------
    private void Start()
    {
        // 컨트롤러 자동 찾기
        if (_controller == null)
            _controller = FindObjectOfType<QuickSlotController>();

        // ★ 이벤트 구독 (Logic -> View)
        if (_controller != null)
        {
            _controller.OnQuickSlotChanged += UpdateSlotIcon; // 아이콘 변경
            _controller.OnSlotUsed += SelectSlot;             // 애니메이션 재생
        }

        InitializeSlots();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (_controller != null)
        {
            _controller.OnQuickSlotChanged -= UpdateSlotIcon;
            _controller.OnSlotUsed -= SelectSlot;
        }
    }

    // Update()의 Input 로직은 제거됨 (Controller가 담당)

    // ---------------------------------------------------------
    // Core Logic
    // ---------------------------------------------------------
    private void InitializeSlots()
    {
        for (int i = 0; i < _slotRects.Length; i++)
        {
            if (_slotRects[i] == null) continue;

            // 위치/회전 초기화
            _slotRects[i].anchoredPosition = new Vector2(_slotRects[i].anchoredPosition.x, _defaultY);
            _slotRects[i].localRotation = Quaternion.Euler(0, 0, _defaultRotation);

            // 이미지 초기화 (빈 상태로 시작)
            if (i < _slotImages.Length && _slotImages[i] != null)
            {
                _slotImages[i].sprite = null;
                _slotImages[i].enabled = false; // 아이콘 끄기

                Color color = _slotImages[i].color;
                color.a = _defaultAlpha;
                _slotImages[i].color = color;
            }
        }
        _currentIndex = -1;
    }

    // ★ 아이콘 업데이트 (Controller에서 호출)
    private void UpdateSlotIcon(int index, ItemData item)
    {
        if (index < 0 || index >= _slotImages.Length) return;

        Image targetImage = _slotImages[index];
        if (targetImage == null) return;

        if (item != null)
        {
            targetImage.sprite = item.icon;
            targetImage.enabled = true;
            // preserveAspect는 필요 시 true로 고정하거나 ItemData에 추가
            targetImage.preserveAspect = true;
        }
        else
        {
            targetImage.sprite = null;
            targetImage.enabled = false;
        }
    }

    /// <summary>
    /// 슬롯 선택 애니메이션 (Controller에서 호출)
    /// </summary>
    private void SelectSlot(int index)
    {
        if (_slotRects == null || index < 0 || index >= _slotRects.Length) return;
        if (_currentIndex == index) return; // 이미 선택된 거면 패스

        // 1. 이전에 선택된 슬롯 비활성화 (내려가기)
        if (_currentIndex != -1 && _currentIndex < _slotRects.Length)
        {
            int oldIndex = _currentIndex;
            RectTransform oldRect = _slotRects[oldIndex];
            Image oldImage = (oldIndex < _slotImages.Length) ? _slotImages[oldIndex] : null;

            if (oldRect != null)
            {
                oldRect.DOKill();
                oldRect.DOAnchorPosY(_defaultY, _animDuration);
                oldRect.DORotate(new Vector3(0, 0, _defaultRotation), _animDuration);
            }

            if (oldImage != null)
            {
                oldImage.DOKill();
                oldImage.DOFade(_defaultAlpha, _animDuration);
            }
        }

        // 2. 새로운 슬롯 활성화 (올라오기)
        _currentIndex = index;
        RectTransform newRect = _slotRects[_currentIndex];
        Image newImage = (_currentIndex < _slotImages.Length) ? _slotImages[_currentIndex] : null;

        if (newRect != null)
        {
            newRect.DOKill();
            newRect.DOAnchorPosY(_selectedY, _animDuration).SetEase(_animEase);
            newRect.DORotate(new Vector3(0, 0, _selectedRotation), _animDuration).SetEase(_animEase);
        }

        if (newImage != null)
        {
            newImage.DOKill();
            newImage.DOFade(_selectedAlpha, _animDuration);
        }
    }
}