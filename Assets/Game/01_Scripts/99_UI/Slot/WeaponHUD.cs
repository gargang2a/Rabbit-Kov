using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트 제어
using DG.Tweening;    // DOTween 애니메이션

public class WeaponHUD : MonoBehaviour
{
    // ---------------------------------------------------------
    // 데이터 구조 정의
    // ---------------------------------------------------------
    [System.Serializable]
    public class WeaponUIData
    {
        public string name;                // 무기 이름
        public Sprite icon;                // 아이콘 스프라이트
        public bool preserveAspect = true; // 비율 유지 여부
    }

    // ---------------------------------------------------------
    // 멤버 변수 (Strict Convention: _camelCase)
    // ---------------------------------------------------------
    [Header("1. UI References")]
    [SerializeField] private RectTransform[] _slotRects; // 위치/회전을 제어할 슬롯
    [SerializeField] private Image[] _slotImages;        // 알파값을 제어할 이미지

    [Header("2. Weapon Data")]
    [SerializeField] private WeaponUIData[] _weaponDataList;

    [Header("3. Position Settings")]
    [SerializeField] private float _selectedY = 30f;
    [SerializeField] private float _defaultY = 0f;

    [Header("4. Rotation Settings (Z-Axis)")]
    [SerializeField] private float _selectedRotation = 0f;   // 선택됨: 0도
    [SerializeField] private float _defaultRotation = -15f;  // 기본: -15도

    [Header("5. Alpha Settings (0~1)")]
    [Tooltip("255 = 1.0")]
    [SerializeField, Range(0f, 1f)] private float _selectedAlpha = 1f;

    [Tooltip("225 = ~0.88")]
    [SerializeField, Range(0f, 1f)] private float _defaultAlpha = 0.882f; // 225/255

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
        InitializeSlots();
    }

    private void Update()
    {
        HandleInput();

        // 개발용: R키로 실시간 갱신 테스트
        if (Input.GetKeyDown(KeyCode.R)) InitializeSlots();
    }

    // ---------------------------------------------------------
    // Core Logic
    // ---------------------------------------------------------
    private void InitializeSlots()
    {
        // 1. 슬롯 상태 초기화 (모두 비선택 상태로 시작)
        for (int i = 0; i < _slotRects.Length; i++)
        {
            if (_slotRects[i] == null) continue;

            // 위치 초기화
            _slotRects[i].anchoredPosition = new Vector2(_slotRects[i].anchoredPosition.x, _defaultY);

            // 회전 초기화 (Quaternion 변환)
            _slotRects[i].localRotation = Quaternion.Euler(0, 0, _defaultRotation);

            // 이미지 및 알파 초기화
            if (i < _slotImages.Length && _slotImages[i] != null)
            {
                // 데이터 바인딩
                if (i < _weaponDataList.Length)
                {
                    WeaponUIData data = _weaponDataList[i];
                    _slotImages[i].sprite = data.icon;
                    _slotImages[i].preserveAspect = data.preserveAspect;
                    _slotImages[i].enabled = (data.icon != null);
                }

                // 알파값 초기화 (Color 구조체 수정)
                Color color = _slotImages[i].color;
                color.a = _defaultAlpha;
                _slotImages[i].color = color;
            }
        }

        _currentIndex = -1; // 아무것도 선택되지 않은 상태
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
    }

    /// <summary>
    /// 슬롯 선택 애니메이션 (이동, 회전, 알파)
    /// </summary>
    private void SelectSlot(int index)
    {
        // 유효성 검사
        if (_slotRects == null || index < 0 || index >= _slotRects.Length) return;
        if (_currentIndex == index) return;

        // -----------------------------------------------------
        // 1. 이전에 선택된 슬롯 비활성화 (내려가기)
        // -----------------------------------------------------
        if (_currentIndex != -1 && _currentIndex < _slotRects.Length)
        {
            int oldIndex = _currentIndex; // 캡처
            RectTransform oldRect = _slotRects[oldIndex];
            Image oldImage = (oldIndex < _slotImages.Length) ? _slotImages[oldIndex] : null;

            if (oldRect != null)
            {
                oldRect.DOKill(); // 기존 트윈 중단

                // Y축 이동 (내려감)
                oldRect.DOAnchorPosY(_defaultY, _animDuration);

                // Z축 회전 (기울어짐 -15도)
                oldRect.DORotate(new Vector3(0, 0, _defaultRotation), _animDuration);
            }

            if (oldImage != null)
            {
                oldImage.DOKill();
                // 알파값 변경 (225/255)
                oldImage.DOFade(_defaultAlpha, _animDuration);
            }
        }

        // -----------------------------------------------------
        // 2. 새로운 슬롯 활성화 (올라오기)
        // -----------------------------------------------------
        _currentIndex = index;
        RectTransform newRect = _slotRects[_currentIndex];
        Image newImage = (_currentIndex < _slotImages.Length) ? _slotImages[_currentIndex] : null;

        if (newRect != null)
        {
            newRect.DOKill();

            // Y축 이동 (올라옴)
            newRect.DOAnchorPosY(_selectedY, _animDuration).SetEase(_animEase);

            // Z축 회전 (바로 섬 0도)
            newRect.DORotate(new Vector3(0, 0, _selectedRotation), _animDuration).SetEase(_animEase);
        }

        if (newImage != null)
        {
            newImage.DOKill();
            // 알파값 변경 (255/255 = 1.0)
            newImage.DOFade(_selectedAlpha, _animDuration);
        }
    }
}