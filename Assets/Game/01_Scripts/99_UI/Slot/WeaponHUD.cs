using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WeaponHUD : MonoBehaviour
{
    // ... (변수 선언부는 기존과 동일) ...
    [Header("1. References")]
    [SerializeField] private QuickSlotController _controller;
    [SerializeField] private RectTransform[] _slotRects;
    [SerializeField] private Image[] _slotImages;

    [Header("3. Position Settings")]
    [SerializeField] private float _selectedY = 30f;
    [SerializeField] private float _defaultY = 0f;

    [Header("4. Rotation Settings")]
    [SerializeField] private float _selectedRotation = 0f;
    [SerializeField] private float _defaultRotation = -15f;

    [Header("5. Alpha Settings")]
    [SerializeField, Range(0f, 1f)] private float _selectedAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float _defaultAlpha = 0.882f;

    [Header("6. Animation Settings")]
    [SerializeField] private float _animDuration = 0.2f;
    [SerializeField] private Ease _animEase = Ease.OutBack;

    private int _currentIndex = -1;

    private void Start()
    {
        if (_controller == null) _controller = FindObjectOfType<QuickSlotController>();

        if (_controller != null)
        {
            _controller.OnQuickSlotChanged += UpdateSlotIcon;
            _controller.OnSlotUsed += SelectSlot;
        }

        InitializeSlots();
    }

    private void OnDestroy()
    {
        if (_controller != null)
        {
            _controller.OnQuickSlotChanged -= UpdateSlotIcon;
            _controller.OnSlotUsed -= SelectSlot;
        }
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < _slotRects.Length; i++)
        {
            if (_slotRects[i] == null) continue;
            _slotRects[i].anchoredPosition = new Vector2(_slotRects[i].anchoredPosition.x, _defaultY);
            _slotRects[i].localRotation = Quaternion.Euler(0, 0, _defaultRotation);

            if (i < _slotImages.Length && _slotImages[i] != null)
            {
                _slotImages[i].sprite = null;
                _slotImages[i].enabled = false;
                Color color = _slotImages[i].color;
                color.a = _defaultAlpha;
                _slotImages[i].color = color;
            }
        }
        _currentIndex = -1;
    }

    private void UpdateSlotIcon(int index, ItemData item)
    {
        if (index < 0 || index >= _slotImages.Length) return;
        Image targetImage = _slotImages[index];
        if (targetImage == null) return;

        if (item != null)
        {
            targetImage.sprite = item.icon;
            targetImage.enabled = true;
            targetImage.preserveAspect = true;
        }
        else
        {
            targetImage.sprite = null;
            targetImage.enabled = false;
        }
    }

    // ★ [수정됨] 슬롯 선택/해제 애니메이션
    private void SelectSlot(int index)
    {
        // 범위 체크 (단, -1은 "해제" 신호이므로 허용)
        if (_slotRects == null) return;
        if (index < -1 || index >= _slotRects.Length) return;

        // 이미 선택된 상태면 무시 (단, -1이 들어오면 무조건 실행해야 함)
        if (_currentIndex == index && index != -1) return;

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

        // ★ [추가] 만약 index가 -1이면 "모두 해제"이므로 여기서 끝냄
        if (index == -1)
        {
            _currentIndex = -1;
            return;
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