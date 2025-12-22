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
    [Header("UI Elements")]
    [SerializeField] private RectTransform _dialoguePanelRect;
    [SerializeField] private TextMeshProUGUI _npcNameText;
    [SerializeField] private TextMeshProUGUI _dialogueText;
    [SerializeField] private RectTransform _nextCursorRect;

    [Header("Shop Buttons")]
    [SerializeField] private GameObject _actionButtonsPanel;

    [Header("Panel Animation Settings")]
    [SerializeField] private float _slideDuration = 0.4f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InBack;
    [SerializeField] private float _hiddenPosY = -500f; // 화면 밖 위치 (하단)
    [SerializeField] private float _visiblePosY = 100f;  // 화면 안 위치

    [Header("Cursor Animation")]
    [SerializeField] private float _cursorMoveDistance = 10f;
    [SerializeField] private float _cursorSpeed = 0.8f;

    [Header("Quest Selection (Keyboard Only)")]
    [SerializeField] private GameObject _selectionPanel;     // 선택창 부모 패널
    [SerializeField] private TextMeshProUGUI _acceptText;   // "수락" 텍스트
    [SerializeField] private TextMeshProUGUI _refuseText;   // "거절" 텍스트
    [SerializeField] private RectTransform _selectionArrow; // 선택된 곳을 가리키는 화살표
    [SerializeField] private float _arrowXOffset = 100f;   // 화살표가 글자로부터 떨어질 거리

    [Header("Audio Settings")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private float _minPitch = 0.95f;
    [SerializeField] private float _maxPitch = 1.05f;
    [SerializeField] private int _soundFrequency = 2;
    private List<AudioClip> _activeVoices;

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

    public float typingSpeed = 0.05f;

    public bool IsDialogueOpen() => _isOpen;
    private void Awake()
    {
        if (_dialoguePanelRect != null)
        {
            _dialoguePanelRect.anchoredPosition = new Vector2(0, _hiddenPosY);
            _dialoguePanelRect.gameObject.SetActive(false);
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
    public static Action<bool> OnDialogueStateChanged;
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
    public void ShowDialogueList(string npcName, List<string> messages, Action onAllHideComplete, List<AudioClip> voices)
    {
        _activeVoices = voices;
        if (_isAnimating) return;

        OnDialogueStateChanged?.Invoke(true);
        _currentMessages = messages;
        _messageIndex = 0;
        _onHideComplete = onAllHideComplete;
        _isOpen = true;
        _isAnimating = true;

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
                yield return new WaitForSeconds(typingSpeed * 2f);
            }
            else
            {
                yield return new WaitForSeconds(typingSpeed);
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
    public void HideDialogue()
    {
        if (!_isOpen) return;

        _isOpen = false;
        _isAnimating = true;

        StopCursorAnimation();
        if (_selectionPanel != null) _selectionPanel.SetActive(false);

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
                    OnDialogueStateChanged?.Invoke(false);

                    Time.timeScale = 1f;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                });
        }
    }
}