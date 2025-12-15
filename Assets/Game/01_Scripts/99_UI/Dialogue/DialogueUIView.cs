using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용 권장
using DG.Tweening;

public class DialogueUIView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform _dialoguePanelRect; // 대화창 전체 패널
    [SerializeField] private RectTransform _nextCursorRect;    // 우측 하단 삼각형 커서
    [SerializeField] private TextMeshProUGUI _nameText;        // NPC 이름 (선택)
    [SerializeField] private TextMeshProUGUI _bodyText;        // 대화 내용

    [Header("Animation Settings")]
    [SerializeField] private float _slideDuration = 0.4f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InBack;

    [Header("Position Settings")]
    [SerializeField] private float _hiddenPosY = -300f; // 화면 아래 숨겨질 Y 좌표
    [SerializeField] private float _visiblePosY = 50f;  // 화면에 보일 Y 좌표 (바닥에서 약간 띄움)

    [Header("Cursor Animation")]
    [SerializeField] private float _cursorMoveDistance = 10f; // 커서가 위아래로 움직일 거리
    [SerializeField] private float _cursorSpeed = 0.8f;       // 커서 반복 속도

    // 내부 상태
    private bool _isOpen = false;
    private Tween _cursorTween; // 커서 트윈을 제어하기 위한 변수

    private void Awake()
    {
        // 초기화: 대화창을 숨겨진 위치로 이동
        if (_dialoguePanelRect != null)
        {
            _dialoguePanelRect.anchoredPosition = new Vector2(0, _hiddenPosY);
        }

        // 커서는 처음에 숨김 처리하거나 애니메이션 멈춤
        if (_nextCursorRect != null)
        {
            _nextCursorRect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 테스트용: T 키로 대화창 열고 닫기
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (_isOpen) HideDialogue();
            else ShowDialogue("Duckov", "이 구역은 밤이 되면 위험해. Ghost가 나타나거든.\n준비는 되었나?");
        }
    }

    public void ShowDialogue(string npcName, string content)
    {
        if (_isOpen) return; // 이미 열려있으면 무시 (혹은 내용만 갱신)

        _isOpen = true;

        // 텍스트 설정
        if (_nameText != null) _nameText.text = npcName;
        if (_bodyText != null) _bodyText.text = content;

        // 1. 대화창 등장 애니메이션
        _dialoguePanelRect.DOKill();
        _dialoguePanelRect.DOAnchorPosY(_visiblePosY, _slideDuration)
            .SetEase(_openEase)
            .OnComplete(StartCursorAnimation); // 등장이 끝나면 커서 애니메이션 시작
    }

    public void HideDialogue()
    {
        if (!_isOpen) return;

        _isOpen = false;

        // 커서 애니메이션 중지 및 숨김
        StopCursorAnimation();

        // 2. 대화창 퇴장 애니메이션
        _dialoguePanelRect.DOKill();
        _dialoguePanelRect.DOAnchorPosY(_hiddenPosY, _slideDuration)
            .SetEase(_closeEase);
    }

    private void StartCursorAnimation()
    {
        if (_nextCursorRect == null) return;

        _nextCursorRect.gameObject.SetActive(true);

        // 기존 트윈이 있다면 제거
        _cursorTween?.Kill();

        // 현재 위치에서 위로 둥둥 떠다니는 애니메이션 (Yoyo)
        // Relative()를 사용하여 현재 위치 기준으로 이동
        _cursorTween = _nextCursorRect.DOAnchorPosY(_cursorMoveDistance, _cursorSpeed)
            .SetRelative(true)
            .SetLoops(-1, LoopType.Yoyo) // 무한 반복, 갔다 돌아오기
            .SetEase(Ease.InOutSine);    // 부드러운 사인파 움직임
    }

    private void StopCursorAnimation()
    {
        if (_nextCursorRect == null) return;

        _cursorTween?.Kill();
        _nextCursorRect.gameObject.SetActive(false);

        // 위치 초기화 (필요하다면 원래 Y값으로 복구하는 로직 추가 가능)
    }
}