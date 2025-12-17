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
    [SerializeField, Range(0f, 200f)] private float _visibleWidthClosed = 50f;

    [Tooltip("열렸을 때 우측 끝에서의 오프셋 (0=딱맞음, 양수=덜나옴, 음수=더나옴)")]
    [SerializeField] private float _openXOffset = 0f;

    [Header("Animation Settings")]
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InCubic;

    [Header("Inventory Logic")]
    [SerializeField] private Transform _slotParent; // 칸들이 생성될 부모 (그리드 레이아웃)
    [SerializeField] private GameObject _slotPrefab; // 칸 프리팹
    [SerializeField] private int _maxSlots = 20;

    private List<InventorySlot> _slots = new List<InventorySlot>();

    private bool _isOpen = false;
    private Canvas _myCanvas;
    private void Start()
    {
        // 시작 시 슬롯 생성
        for (int i = 0; i < _maxSlots; i++)
        {
            GameObject newSlot = Instantiate(_slotPrefab, _slotParent);
            _slots.Add(newSlot.GetComponent<InventorySlot>());
        }
    }
    private void Awake()
    {
        _myCanvas = GetComponentInParent<Canvas>();
        if (_inventoryPanel == null)
            _inventoryPanel = GetComponent<RectTransform>();

        // ★ [중요] 시작 시 레이아웃 강제 갱신 (해상도에 따른 정확한 너비 계산 보장)
        LayoutRebuilder.ForceRebuildLayoutImmediate(_inventoryPanel);

        // 초기화: 닫힌 상태
        _isOpen = false;
        _inventoryPanel.anchoredPosition = new Vector2(CalculateClosedXPos(), 0);
    }
    private void Update()
    {
        // 일시정지 중일 때는 인벤토리나 스탯창의 어떠한 입력도 받지 않음
        if (Time.timeScale == 0) return;

        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleStatus();
        }
    }
    // 아이템 습득 시 호출할 함수
    public bool TryAddItem(ItemData item)
    {
        foreach (var slot in _slots)
        {
            if (slot.IsEmpty)
            {
                slot.AddItem(item);
                return true;
            }
        }
        Debug.Log("인벤토리가 가득 찼습니다.");
        return false;
    }
    // --- Public Methods ---
    public void ToggleStatus() // 함수명을 상황에 맞게 수정
    {
        if (_isOpen) Close();
        else Open();
    }
    private float CalculateOpenXPos()
    {
        // Pivot이 (1, 0.5)일 때 0이면 딱 맞게 열림
        return _openXOffset;
    }
    private float CalculateClosedXPos()
    {
        // ★ [핵심] 변수에 저장된 값이 아닌, 현재 프레임의 실제 너비를 사용
        float currentWidth = _inventoryPanel.rect.width;

        // Pivot이 (1, 0.5) 즉 우측 기준일 때:
        // X가 0이면 화면 우측 끝.
        // X가 Width이면 화면 밖으로 완전히 나감.
        // 따라서 (Width - 보이는 양) 만큼 이동하면 '보이는 양'만 남고 나감.
        return currentWidth - _visibleWidthClosed;
    }
    public void Open()
    {
        // ★ 수정: 다른 창이 이미 열려 있다면 열기 불가
        if (_isOpen || (UIManager.Instance != null && UIManager.Instance.IsAnyWindowOpen))
            return;

        _isOpen = true;

        // ★ 추가: 상태 등록
        if (UIManager.Instance != null)
            UIManager.Instance.SetWindowStatus(true);

        _inventoryPanel.DOKill(); // 기존 애니메이션 중단

        if (UIManager.Instance != null && _myCanvas != null)
        {
            UIManager.Instance.BringToFront(_myCanvas);
        }

        // ★ [핵심 추가] 열리는 순간 이 UI를 부모의 가장 아래(화면상 맨 위)로 보냄
        _inventoryPanel.SetAsLastSibling();

        // ★ [최적화] 열릴 때마다 목표 위치 재계산 (해상도 변경 대응)
        float targetX = CalculateOpenXPos();

        _inventoryPanel.DOAnchorPosX(targetX, _slideDuration)
            .SetEase(_openEase)
            .SetUpdate(true); // TimeScale이 0이어도(일시정지) UI는 작동하도록 설정
    }
    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;

        // ★ 추가: 상태 등록 해제
        if (UIManager.Instance != null)
            UIManager.Instance.SetWindowStatus(false);

        _inventoryPanel.DOKill();
        // ★ [최적화] 닫힐 때마다 목표 위치 재계산
        float targetX = CalculateClosedXPos();
        _inventoryPanel.DOAnchorPosX(targetX, _slideDuration)
            .SetEase(_closeEase)
            .SetUpdate(true);
    }
}