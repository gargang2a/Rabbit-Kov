using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class DialogueUIView : MonoBehaviour
{
    [Header("UI Elements / UI 요소")]
    [SerializeField] private RectTransform _dialoguePanelRect;
    [SerializeField] private TextMeshProUGUI _npcNameText;
    [SerializeField] private TextMeshProUGUI _dialogueText;
    [SerializeField] private RectTransform _nextCursorRect;

    [Header("Shop Buttons / 행동 버튼 패널")]
    [SerializeField] private GameObject _actionButtonsPanel;

    [Header("Panel Animation Settings")]
    [SerializeField] private float _slideDuration = 0.4f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InBack;
    [SerializeField] private float _hiddenPosY = -500f;
    [SerializeField] private float _visiblePosY = 100f;

    [Header("External UI Integration")]
    [SerializeField] private RectTransform _bottomPanelRect;
    [SerializeField] private float _bottomHiddenPosY = -300f;
    [SerializeField] private float _bottomVisiblePosY = 0f;

    [Header("Cursor Animation")]
    [SerializeField] private float _cursorMoveDistance = 10f;
    [SerializeField] private float _cursorSpeed = 0.8f;

    [Header("Quest Selection")]
    [SerializeField] private GameObject _selectionPanel;
    [SerializeField] private TextMeshProUGUI _acceptText;
    [SerializeField] private TextMeshProUGUI _refuseText;
    [SerializeField] private RectTransform _selectionArrow;
    [SerializeField] private float _arrowXOffset = 100f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private float _minPitch = 0.8f;
    [SerializeField] private float _maxPitch = 1.2f;
    [SerializeField] private int _soundFrequency = 1;
    [SerializeField] private float _typingSpeed = 0.05f;

    private List<AudioClip> _activeVoices;
    private bool _isSelectionMode = false;
    private int _currentSelectedIndex = 0;
    private Action _onAccept;
    private Action _onRefuse;

    private bool _isOpen = false;
    private bool _isAnimating = false; // 애니메이션 중인지 여부
    private List<string> _currentMessages;
    private int _messageIndex = 0;
    private Action _onHideComplete;
    private Coroutine _typingCoroutine;
    private Tween _cursorTween;

    public bool IsDialogueOpen() => _isOpen;

    // 대화 상태 변화 이벤트
    public static Action<bool> OnDialogueStateChanged;

    private void Awake()
    {
        if (_dialoguePanelRect != null)
        {
            _dialoguePanelRect.anchoredPosition = new Vector2(0, _hiddenPosY);
            _dialoguePanelRect.gameObject.SetActive(false);
        }
        // 초기화 로직 유지...
        if (_bottomPanelRect != null) _bottomPanelRect.anchoredPosition = new Vector2(0, _bottomVisiblePosY);
        if (_nextCursorRect != null) _nextCursorRect.gameObject.SetActive(false);
        if (_selectionPanel != null) _selectionPanel.SetActive(false);
    }

    private void Update()
    {
        // 애니메이션 중일 때는 키 입력 방지 (선택지 이동 등)
        if (!_isOpen || _isAnimating) return;

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

    // ★ [핵심 수정] 대화창 열기 로직 개선
    public void ShowDialogueList(string npcName, List<string> messages, Action onAllHideComplete, List<AudioClip> voices)
    {
        _activeVoices = voices;

        // 1. 기존 애니메이션이 있다면 즉시 중단 (닫히던 중이라도 다시 열리게 함)
        if (_dialoguePanelRect != null) _dialoguePanelRect.DOKill();
        if (_bottomPanelRect != null) _bottomPanelRect.DOKill();

        // 2. 상태 초기화
        OnDialogueStateChanged?.Invoke(true);
        _currentMessages = messages;
        _messageIndex = 0;
        _onHideComplete = onAllHideComplete;
        _isOpen = true;
        _isAnimating = true;

        // 선택창이 켜져있었다면 끄기
        HideActionButtons();

        // 3. 외부 UI 숨기기
        AnimateBottomPanelIn();

        // 4. 대화 패널 활성화 및 애니메이션 시작
        _dialoguePanelRect.gameObject.SetActive(true);

        // 주의: anchoredPosition을 강제로 _hiddenPosY로 초기화하지 않음.
        // 현재 위치(닫히던 중간 위치)에서 바로 목표 위치로 이동하여 자연스럽게 연결.
        _dialoguePanelRect.DOAnchorPosY(_visiblePosY, _slideDuration)
            .SetEase(_openEase)
            .OnComplete(() => {
                _isAnimating = false;
                ShowMessage(_currentMessages[_messageIndex], npcName);
            });
    }

    private void AnimateBottomPanelIn()
    {
        if (_bottomPanelRect == null) return;
        _bottomPanelRect.DOAnchorPosY(_bottomHiddenPosY, _slideDuration).SetEase(_openEase);
    }

    private void AnimateBottomPanelOut()
    {
        if (_bottomPanelRect == null) return;
        _bottomPanelRect.DOAnchorPosY(_bottomVisiblePosY, _slideDuration).SetEase(_closeEase);
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
                if (charCount % _soundFrequency == 0) PlayTypingSound();
            }

            // 문장 부호에서 약간 더 대기
            float waitTime = (letter == '.' || letter == '?' || letter == '!' || letter == ',')
                ? _typingSpeed * 2f : _typingSpeed;

            yield return new WaitForSeconds(waitTime);
        }
        _typingCoroutine = null;
        StartCursorAnimation();
    }

    private void PlayTypingSound()
    {
        if (_audioSource == null || _activeVoices == null || _activeVoices.Count == 0) return;
        int randomIndex = UnityEngine.Random.Range(0, _activeVoices.Count);
        AudioClip selectedClip = _activeVoices[randomIndex];
        if (selectedClip != null)
        {
            _audioSource.pitch = UnityEngine.Random.Range(_minPitch, _maxPitch);
            _audioSource.PlayOneShot(selectedClip);
        }
    }

    public void HandleNextMessage(string npcName)
    {
        // 선택 모드이거나, 패널이 열리는/닫히는 애니메이션 중이면 무시
        if (_isSelectionMode || _isAnimating) return;

        // 타이핑 중이면 즉시 완료
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
            // 대화 끝
            if (_onHideComplete != null)
            {
                _onHideComplete.Invoke();
            }

            // 콜백 실행 후 선택 모드가 아니라면 닫기
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
        _onHideComplete = null; // 중복 실행 방지
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

    public void HideDialogue()
    {
        if (!_isOpen) return;

        _isOpen = false;
        _isAnimating = true; // 닫히는 애니메이션 시작

        StopCursorAnimation();
        if (_selectionPanel != null) _selectionPanel.SetActive(false);

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
                    _isAnimating = false; // 애니메이션 종료
                    _dialogueText.text = "";

                    OnDialogueStateChanged?.Invoke(false);
                });
        }
    }
}