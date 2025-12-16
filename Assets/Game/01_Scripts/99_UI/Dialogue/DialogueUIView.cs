using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class DialogueUIView : MonoBehaviour
{
    [Header("UI Elements")]
    // 사용자님의 Hierarchy 상 'NpcPanel'을 여기에 연결하세요.
    [SerializeField] private GameObject _dialoguePanel;
    [SerializeField] private TextMeshProUGUI _npcNameText;
    [SerializeField] private TextMeshProUGUI _dialogueText;
    [SerializeField] private GameObject _nextCursor;

    [Header("Quest/Shop Buttons")]
    // Hierarchy 상 'QuestPanel' 또는 버튼들을 담은 패널을 연결하세요.
    [SerializeField] private GameObject _actionButtonsPanel;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _refuseButton;

    private bool _isOpen = false;
    private List<string> _currentMessages;
    private int _messageIndex = 0;
    private Action _onHideComplete;
    private Coroutine _typingCoroutine;

    public float typingSpeed = 0.05f;

    public bool IsDialogueOpen() => _isOpen;

    public void ShowDialogueList(string npcName, List<string> messages, Action onAllHideComplete)
    {
        // 인덱스 에러 방지: 리스트가 비어있는지 먼저 확인
        if (messages == null || messages.Count == 0) return;

        _currentMessages = messages;
        _messageIndex = 0;
        _onHideComplete = onAllHideComplete;
        _isOpen = true;

        _dialoguePanel.SetActive(true);
        HideActionButtons();

        ShowMessage(_currentMessages[_messageIndex], npcName);
    }

    private void ShowMessage(string message, string npcName)
    {
        _npcNameText.text = npcName;
        _dialogueText.text = "";
        if (_nextCursor != null) _nextCursor.SetActive(false);

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeMessage(message));
    }

    private IEnumerator TypeMessage(string message)
    {
        foreach (char letter in message.ToCharArray())
        {
            _dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        _typingCoroutine = null;
        if (_nextCursor != null) _nextCursor.SetActive(true);
    }

    public void HandleNextMessage(string npcName)
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _dialogueText.text = _currentMessages[_messageIndex];
            _typingCoroutine = null;
            if (_nextCursor != null) _nextCursor.SetActive(true);
            return;
        }

        _messageIndex++;

        // 🚨 중요: 인덱스 범위 체크 (ArgumentOutOfRangeException 방지)
        if (_messageIndex < _currentMessages.Count)
        {
            ShowMessage(_currentMessages[_messageIndex], npcName);
        }
        else
        {
            // 모든 대화가 끝났을 때 콜백(버튼 띄우기 등) 실행
            if (_onHideComplete != null)
            {
                _onHideComplete.Invoke();
                // 대화창은 유지하고 버튼만 띄워야 하므로 HideDialogue를 여기서 호출하지 않음
            }
            else
            {
                HideDialogue();
            }
        }
    }
    public void ShowActionButtons(Action acceptAction, Action refuseAction)
    {
        if (_actionButtonsPanel != null) _actionButtonsPanel.SetActive(true);
        if (_nextCursor != null) _nextCursor.SetActive(false);

        _acceptButton.onClick.RemoveAllListeners();
        _refuseButton.onClick.RemoveAllListeners();

        _acceptButton.onClick.AddListener(() => {
            acceptAction?.Invoke();
            HideDialogue();
        });

        _refuseButton.onClick.AddListener(() => {
            refuseAction?.Invoke();
            HideDialogue();
        });
    }

    public void HideActionButtons()
    {
        if (_actionButtonsPanel != null) _actionButtonsPanel.SetActive(false);
    }

    public void HideDialogue()
    {
        _dialoguePanel.SetActive(false);
        HideActionButtons();
        _isOpen = false;
        _onHideComplete = null;
    }
}