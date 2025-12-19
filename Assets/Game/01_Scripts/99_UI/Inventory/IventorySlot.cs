// InventorySlot.cs 파일 전체 코드 (좌클릭/우클릭 사운드 분리 및 최종 기능 통합)

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

// IPointerClickHandler 추가
public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _btn;

    [Header("Audio Settings")]
    [Tooltip("아이템 사용/장착 시 재생")]
    [SerializeField] private AudioClip _leftClickSound; // ★★★ 좌클릭 전용 사운드
    [Tooltip("아이템 버리기/교체 시 재생")]
    [SerializeField] private AudioClip _rightClickSound; // ★★★ 우클릭 전용 사운드
    [Range(0f, 0.2f)]
    [SerializeField] private float _pitchRandomness = 0.05f;

    private ItemData _item;
    private Color _initialColor;

    public ItemData Item => _item;
    public bool IsEmpty => _item == null;

    private void Awake()
    {
        // Button의 Target Graphic의 초기 색상을 저장합니다.
        if (_btn != null && _btn.targetGraphic != null)
        {
            _initialColor = _btn.targetGraphic.color;
        }
        else if (_iconImage != null)
        {
            _initialColor = _iconImage.color;
        }
    }

    // ==========================================
    // 1. 슬롯 채우기 (InventoryUI에서 호출됨)
    // ==========================================
    public void SetItem(ItemData newItem)
    {
        _item = newItem;
        _iconImage.sprite = newItem.icon;

        var color = _iconImage.color;
        color.a = 1f;
        _iconImage.color = color;

        _iconImage.enabled = true;
    }

    // ==========================================
    // 2. 슬롯 비우기 (InventoryUI에서 호출됨)
    // ==========================================
    public void ClearSlot()
    {
        _item = null;
        _iconImage.sprite = null;

        var color = _iconImage.color;
        color.a = 0f;
        _iconImage.color = color;

        _iconImage.enabled = false;
    }

    // 3. 클릭 이벤트 (좌/우클릭 로직 통합)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_item == null || InventoryUI.Instance == null) return;

        AudioClip soundToPlay = null; // 재생할 사운드를 저장할 변수

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 좌클릭: 사용/장착
            soundToPlay = _leftClickSound;
            InventoryUI.Instance.OnItemClick(_item);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 우클릭: 버리기
            soundToPlay = _rightClickSound;
            InventoryUI.Instance.OnItemRightClick(this, _item);
        }

        // 사운드 재생
        if (GlobalAudioManager.Instance != null && soundToPlay != null)
        {
            GlobalAudioManager.Instance.PlaySFX(soundToPlay, _pitchRandomness);
        }

        // 클릭 시 눌림 효과 코루틴 실행
        StartCoroutine(ClickVisualRoutine());
    }

    // 4. 마우스 호버링 (Highlight 상태 복구)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.SetHoveredSlot(this);
        }

        // 하이라이트 색상 적용
        if (_btn != null && _btn.interactable)
        {
            SetColor(_btn.colors.highlightedColor);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.SetHoveredSlot(null);
        }

        // 초기 색상으로 복귀
        if (_btn != null && _btn.interactable)
        {
            SetColor(_initialColor);
        }
    }

    // 상태에 따라 색상을 설정하는 헬퍼 메서드
    private void SetColor(Color color)
    {
        if (_btn != null && _btn.targetGraphic != null)
        {
            _btn.targetGraphic.color = color;
        }
        else if (_iconImage != null)
        {
            _iconImage.color = color;
        }
    }

    // 클릭 시 눌림 효과(Pressed)를 짧게 띄우는 코루틴
    private IEnumerator ClickVisualRoutine()
    {
        if (_btn == null || !_btn.interactable) yield break;

        Color pressedColor = _btn.colors.pressedColor;
        SetColor(pressedColor);

        yield return new WaitForSeconds(0.1f); // 짧은 시간 눌림 상태 유지

        // 눌림 상태 해제 후 마우스 위치에 따라 Highlight 또는 Normal 색상으로 복귀
        if (EventSystem.current != null && RectTransformUtility.RectangleContainsScreenPoint(
            GetComponent<RectTransform>(), Input.mousePosition))
        {
            // 마우스가 여전히 슬롯 위에 있다면 Highlighted Color로 복귀
            SetColor(_btn.colors.highlightedColor);
        }
        else
        {
            // 마우스가 슬롯을 벗어났다면 Normal Color로 복귀
            SetColor(_initialColor);
        }
    }
}