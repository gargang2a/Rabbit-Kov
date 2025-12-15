using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class DialogueUIView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform _dialoguePanelRect;
    [SerializeField] private RectTransform _nextCursorRect;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _bodyText;

    [Header("Animation Settings")]
    [SerializeField] private float _slideDuration = 0.4f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InBack;

    [Header("Position Settings")]
    [SerializeField] private float _hiddenPosY = -300f;
    [SerializeField] private float _visiblePosY = 50f;

    [Header("Cursor Animation")]
    [SerializeField] private float _cursorMoveDistance = 10f;
    [SerializeField] private float _cursorSpeed = 0.8f;

    private bool _isOpen = false;
    private Tween _cursorTween;
    private Vector2 _cursorOriginPos; // 커서의 원래 위치 저장용

    private void Awake()
    {
        if (_dialoguePanelRect != null)
        {
            _dialoguePanelRect.anchoredPosition = new Vector2(0, _hiddenPosY);
        }

        if (_nextCursorRect != null)
        {
            // [추가] 시작할 때 커서의 원래 위치를 기억해둡니다.
            _cursorOriginPos = _nextCursorRect.anchoredPosition;
            _nextCursorRect.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (_isOpen) HideDialogue();
            else ShowDialogue("Duckov", "요섭님 화이팅입니다. \n2줄 2줄 2줄 2줄 2줄 2줄 2줄");
        }
    }

    public void ShowDialogue(string npcName, string content)
    {
        if (_isOpen) return;

        _isOpen = true;

        if (_nameText != null) _nameText.text = npcName;
        if (_bodyText != null) _bodyText.text = content;

        // [수정 1] 패널이 올라오기 전에 커서부터 켜고 애니메이션 시작
        StartCursorAnimation();

        // [수정 2] OnComplete 제거 (커서를 기다리지 않음)
        _dialoguePanelRect.DOKill();
        _dialoguePanelRect.DOAnchorPosY(_visiblePosY, _slideDuration)
            .SetEase(_openEase);
    }

    public void HideDialogue()
    {
        if (!_isOpen) return;

        _isOpen = false;

        // 닫을 때는 커서 애니메이션 끄기
        StopCursorAnimation();

        _dialoguePanelRect.DOKill();
        _dialoguePanelRect.DOAnchorPosY(_hiddenPosY, _slideDuration)
            .SetEase(_closeEase);
    }

    private void StartCursorAnimation()
    {
        if (_nextCursorRect == null) return;

        _nextCursorRect.gameObject.SetActive(true);
        _cursorTween?.Kill();

        // [추가] 애니메이션 시작 전, 위치를 원래대로 리셋 (틀어짐 방지)
        _nextCursorRect.anchoredPosition = _cursorOriginPos;

        // 둥둥 떠다니는 효과 시작
        _cursorTween = _nextCursorRect.DOAnchorPosY(_cursorMoveDistance, _cursorSpeed)
            .SetRelative(true)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void StopCursorAnimation()
    {
        if (_nextCursorRect == null) return;

        _cursorTween?.Kill();
        _nextCursorRect.gameObject.SetActive(false);
    }
}