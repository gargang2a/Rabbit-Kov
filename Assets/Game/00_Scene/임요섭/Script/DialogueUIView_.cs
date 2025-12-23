using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;
using DG.Tweening;

public class DialogueUIView : MonoBehaviour
{
    [Header("UI Elements / UI 요소")]
    [Tooltip("대화 패널 RectTransform (애니메이션 대상)")]
    [SerializeField] private RectTransform _dialoguePanelRect;
    [Tooltip("NPC 이름을 표시할 TextMeshProUGUI")]
    [SerializeField] private TextMeshProUGUI _npcNameText;
    [Tooltip("대사 내용을 표시할 TextMeshProUGUI")]
    [SerializeField] private TextMeshProUGUI _dialogueText;
    [Tooltip("다음 대사 진행을 알리는 커서 RectTransform")]
    [SerializeField] private RectTransform _nextCursorRect;

    [Header("Shop Buttons / 행동 버튼 패널")]
    [Tooltip("수락/거절 등 행동 버튼을 담는 패널 (Inspector에 연결)")]
    [SerializeField] private GameObject _actionButtonsPanel;

    [Header("Panel Animation Settings / 패널 애니메이션 설정")]
    [Tooltip("패널이 슬라이드될 때 소요되는 시간 (초)")]
    [SerializeField] private float _slideDuration = 0.4f;
    [Tooltip("패널 열기 이징 (DOTween Ease)")]
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [Tooltip("패널 닫기 이징 (DOTween Ease)")]
    [SerializeField] private Ease _closeEase = Ease.InBack;
    [Tooltip("숨김 상태일 때 패널 Y 위치 (화면 밖)")]
    [SerializeField] private float _hiddenPosY = -500f; // 화면 밖 위치 (하단)
    [Tooltip("표시 상태일 때 패널 Y 위치 (화면 안)")]
    [SerializeField] private float _visiblePosY = 100f;  // 화면 안 위치

    [Header("External UI Integration / 외부 UI 연동")]
    [Tooltip("대화 시 숨겨야 할 하단 UI 패널 RectTransform")]
    [SerializeField] private RectTransform _bottomPanelRect;
    [Tooltip("하단 UI가 숨겨질 Y 위치 (화면 밖, 아래)")]
    [SerializeField] private float _bottomHiddenPosY = -300f;
    [Tooltip("하단 UI가 표시될 원래 Y 위치 (화면 안)")]
    [SerializeField] private float _bottomVisiblePosY = 0f;

    [Header("Cursor Animation / 커서 애니메이션")]
    [Tooltip("커서가 움직일 상대 거리 (Y축)")]
    [SerializeField] private float _cursorMoveDistance = 10f;
    [Tooltip("커서 애니메이션 주기 (초)")]
    [SerializeField] private float _cursorSpeed = 0.8f;

    [Header("Quest Selection (Keyboard Only) / 선택창 (키보드 전용)")]
    [Tooltip("선택창 전체 부모 패널 (수락/거절 선택 창)")]
    [SerializeField] private GameObject _selectionPanel;     // 선택창 부모 패널
    [Tooltip("\"수락\" 텍스트 (TextMeshProUGUI)")]
    [SerializeField] private TextMeshProUGUI _acceptText;   // "수락" 텍스트
    [Tooltip("\"거절\" 텍스트 (TextMeshProUGUI)")]
    [SerializeField] private TextMeshProUGUI _refuseText;   // "거절" 텍스트
    [Tooltip("선택된 항목을 가리키는 화살표 RectTransform")]
    [SerializeField] private RectTransform _selectionArrow; // 선택된 곳을 가리키는 화살표
    [Tooltip("화살표가 텍스트로부터 떨어진 X축 거리")]
    [SerializeField] private float _arrowXOffset = 100f;   // 화살표가 글자로부터 떨어질 거리

    [Header("Audio Settings / 오디오 설정")]
    [Tooltip("타이핑 효과를 재생할 AudioSource")]
    [SerializeField] private AudioSource _audioSource;
    [Tooltip("타이핑 사운드 재생 시 최소 피치")]
    [SerializeField] private float _minPitch = 0.8f;
    [Tooltip("타이핑 사운드 재생 시 최대 피치")]
    [SerializeField] private float _maxPitch = 1.2f;
    [Tooltip("몇 글자마다 사운드를 재생할지 (예: 1 = 모든 글자마다)")]
    [SerializeField] private int _soundFrequency = 1;
    private List<AudioClip> _activeVoices;

    [Tooltip("타이핑 시 문자당 대기 시간 (초)")]
    [SerializeField] private float _typingSpeed = 0.05f;

    private bool _isSelectionMode = false;   // 현재 선택 모드인지 여부
    private int _currentSelectedIndex = 0;   // 0: 수락, 1: 거절
    private Action _onAccept;                // 수락 시 실행할 함수 저장
    private Action _onRefuse;                // 거절 시 실행할 함수 저장

    private bool _isOpen = false;
    private bool _isAnimating = false;
    private List<string> _currentMessages;
    private int _messageIndex = 0;
    private Action _onHideComplete;
    private Coroutine _typingCoroutine;

    private Tween _cursorTween;
    private Vector2 _cursorOriginPos;

    public bool IsDialogueOpen() => _isOpen;

    // 대화 상태 변화를 외부에 알리는 이벤트 (느슨한 결합 제공)
    public static Action<bool> OnDialogueStateChanged;

    private void Awake()
    {
        if (_dialoguePanelRect != null)
        {
            _dialoguePanelRect.anchoredPosition = new Vector2(0, _hiddenPosY);
            _dialoguePanelRect.gameObject.SetActive(false);
        }
        if (_bottomPanelRect != null)
        {
            _bottomPanelRect.anchoredPosition = new Vector2(0, _bottomVisiblePosY);
        }
        if (_nextCursorRect != null)
        {
            _cursorOriginPos = _nextCursorRect.anchoredPosition;
            _nextCursorRect.gameObject.SetActive(false);
        }
        if (_selectionPanel != null) _selectionPanel.SetActive(false);
        if (_acceptText != null) _acceptText.gameObject.SetActive(false);
        if (_refuseText != null) _refuseText.gameObject.SetActive(false);
        if (_selectionArrow != null) _selectionArrow.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isOpen || _isAnimating) return;

        // E 키 락은 이 스크립트가 아닌, 대화 상태를 구독하는 다른 Interactor 스크립트에서 처리되어야 합니다.
        // 이 스크립트는 UI 제어에만 집중합니다.

        if (_isSelectionMode)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
            {
                _currentSelectedIndex = (_currentSelectedIndex == 0) ? 1 : 0;
                UpdateSelectionUI();
            }
            if (Input.GetKeyDown(KeyCode.F))
            {
                ConfirmSelection();
            }
        }
    }

    private void PlayTypingSound()
    {
        if (_audioSource == null || _activeVoices == null || _activeVoices.Count == 0)
        {
            Debug.LogWarning("AudioSource 또는 Voice 리스트가 비어있습니다!");
            return;
        }
        int randomIndex = UnityEngine.Random.Range(0, _activeVoices.Count);
        AudioClip selectedClip = _activeVoices[randomIndex];
        if (selectedClip != null)
        {
            _audioSource.pitch = UnityEngine.Random.Range(_minPitch, _maxPitch);
            _audioSource.PlayOneShot(selectedClip);
        }
    }

    private void AnimateBottomPanelIn()
    {
        if (_bottomPanelRect == null) return;
        _bottomPanelRect.DOKill();
        // 하단 UI를 화면 밖으로 슬라이드 (숨기기)
        _bottomPanelRect.DOAnchorPosY(_bottomHiddenPosY, _slideDuration)
            .SetEase(_openEase);
    }

    private void AnimateBottomPanelOut()
    {
        if (_bottomPanelRect == null) return;
        _bottomPanelRect.DOKill();
        // 하단 UI를 원래 위치로 슬라이드 (표시)
        _bottomPanelRect.DOAnchorPosY(_bottomVisiblePosY, _slideDuration)
            .SetEase(_closeEase);
    }

    public void ShowDialogueList(string npcName, List<string> messages, Action onAllHideComplete, List<AudioClip> voices)
    {
        _activeVoices = voices;
        if (_isAnimating) return;

        OnDialogueStateChanged?.Invoke(true); // 대화 시작 알림

        _currentMessages = messages;
        _messageIndex = 0;
        _onHideComplete = onAllHideComplete;
        _isOpen = true;
        _isAnimating = true;

        // 외부 UI 숨기기 시작 (Concurrent)
        AnimateBottomPanelIn();

        _dialoguePanelRect.gameObject.SetActive(true);
        _dialoguePanelRect.DOKill();

        _dialoguePanelRect.anchoredPosition = new Vector2(0, _hiddenPosY);

        _dialoguePanelRect.DOAnchorPosY(_visiblePosY, _slideDuration)
            .SetEase(_openEase)
            .OnComplete(() => {
                _isAnimating = false;
                ShowMessage(_currentMessages[_messageIndex], npcName);
            });
    }

    private IEnumerator EnableInputAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _isAnimating = false;
    }

    private void ShowMessage(string message, string npcName)
    {
        if (_npcNameText != null) _npcNameText.text = npcName;
        _dialogueText.text = "";

        StopCursorAnimation();

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeMessage(message));
    }

    private IEnumerator TypeMessage(string message)
    {
        int charCount = 0;
        foreach (char letter in message.ToCharArray())
        {
            _dialogueText.text += letter;

            if (letter != ' ')
            {
                charCount++;
                if (charCount % _soundFrequency == 0)
                {
                    PlayTypingSound();
                }
            }
            if (letter == '.' || letter == '?' || letter == '!' || letter == ',')
            {
                yield return new WaitForSeconds(_typingSpeed * 2f);
            }
            else
            {
                yield return new WaitForSeconds(_typingSpeed);
            }
        }
        _typingCoroutine = null;
        StartCursorAnimation();
    }

    public void HandleNextMessage(string npcName)
    {
        if (_isSelectionMode || _isAnimating) return;

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _dialogueText.text = _currentMessages[_messageIndex];
            _typingCoroutine = null;
            StartCursorAnimation();
            return;
        }

        _messageIndex++;

        if (_messageIndex < _currentMessages.Count)
        {
            ShowMessage(_currentMessages[_messageIndex], npcName);
        }
        else
        {
            if (_onHideComplete != null)
            {
                _onHideComplete.Invoke();
            }
            if (!_isSelectionMode)
            {
                HideDialogue();
            }
        }
    }

    private void StartCursorAnimation()
    {
        if (_nextCursorRect == null) return;
        _nextCursorRect.gameObject.SetActive(true);
        _cursorTween?.Kill();
        _cursorTween = _nextCursorRect.DOAnchorPosY(_cursorMoveDistance, _cursorSpeed)
            .SetRelative(true).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    private void StopCursorAnimation()
    {
        if (_nextCursorRect == null) return;
        _cursorTween?.Kill();
        _nextCursorRect.gameObject.SetActive(false);
    }

    public void ShowActionButtons(Action acceptAction, Action refuseAction)
    {
        if (_selectionPanel == null) return;

        StopCursorAnimation();

        _selectionPanel.SetActive(true);
        if (_acceptText != null) _acceptText.gameObject.SetActive(true);
        if (_refuseText != null) _refuseText.gameObject.SetActive(true);
        if (_selectionArrow != null) _selectionArrow.gameObject.SetActive(true);

        _isSelectionMode = true;
        _onAccept = acceptAction;
        _onRefuse = refuseAction;
        _currentSelectedIndex = 0;

        UpdateSelectionUI();
        _onHideComplete = null;
    }

    private void UpdateSelectionUI()
    {
        if (_selectionArrow == null || _acceptText == null || _refuseText == null) return;

        TextMeshProUGUI targetText = (_currentSelectedIndex == 0) ? _acceptText : _refuseText;
        TextMeshProUGUI nonTargetText = (_currentSelectedIndex == 0) ? _refuseText : _acceptText;

        _selectionArrow.SetParent(targetText.transform);
        _selectionArrow.anchoredPosition = new Vector2(_arrowXOffset, 0);

        targetText.color = Color.yellow;
        nonTargetText.color = Color.white;
    }

    private void ConfirmSelection()
    {
        _isSelectionMode = false;
        HideActionButtons();

        if (_currentSelectedIndex == 0) _onAccept?.Invoke();
        else _onRefuse?.Invoke();

        HideDialogue();
    }

    public void HideActionButtons()
    {
        if (_selectionPanel != null) _selectionPanel.SetActive(false);
        if (_actionButtonsPanel != null) _actionButtonsPanel.SetActive(false);
        _isSelectionMode = false;
    }

    // ?? [수정] SRP 위반 코드를 제거하고, UI 종료 상태만 외부에 알림
    public void HideDialogue()
    {
        if (!_isOpen) return;

        _isOpen = false;
        _isAnimating = true;

        StopCursorAnimation();
        if (_selectionPanel != null) _selectionPanel.SetActive(false);

        // 외부 UI 다시 표시 시작 (Concurrent)
        AnimateBottomPanelOut();

        if (_dialoguePanelRect != null)
        {
            _dialoguePanelRect.DOKill();
            _dialoguePanelRect.DOAnchorPosY(_hiddenPosY, _slideDuration)
                .SetEase(_closeEase)
                .OnComplete(() =>
                {
                    _dialoguePanelRect.gameObject.SetActive(false);
                    _onHideComplete = null;
                    _isAnimating = false;
                    _dialogueText.text = "";

                    // UI 상태 변화만 외부에 알리고, 게임 상태 제어는 상위 시스템에 위임
                    OnDialogueStateChanged?.Invoke(false);
                });
        }
    }
}