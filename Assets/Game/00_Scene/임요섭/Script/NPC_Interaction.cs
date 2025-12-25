using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Interaction : MonoBehaviour
{
    // 현재 상호작용 중인 NPC를 저장하는 정적 변수
    public static NPC_Interaction ActiveNPC;

    // =========================
    // 퀘스트 상태 열거형
    // =========================
    public enum QuestState
    {
        NOT_STARTED,
        IN_PROGRESS,
        CAN_BE_COMPLETED,
        COMPLETED
    }

    // =========================
    // 퀘스트 상세 정보
    // =========================
    [System.Serializable]
    public class QuestDetails
    {
        [Header("퀘스트 정보")]
        public int questID;
        public string questName;
        [TextArea(0, 15)] public string description;

        [Header("목표")]
        public ItemData goalItem;
        public int requiredAmount;

        [Header("보상")]
        public int rewardCoin;
        public int rewardExp;
        public ItemData rewardItem;
    }

    // =========================
    // NPC 설정
    // =========================
    [Header("NPC 설정")]
    public string npcName = "";
    public KeyCode interactionKey = KeyCode.F;
    public float interactionRange = 3f;

    // ★ [수정] 상호작용 종료 거리 (시작 거리보다 20% 여유를 둠)
    // 플레이어가 경계선에 있을 때 UI가 깜빡거리는 현상 방지
    private float _exitInteractionRange => interactionRange * 1.2f;

    [Header("퀘스트 정보")]
    public QuestDetails availableQuest;
    public QuestState currentQuestState = QuestState.NOT_STARTED;

    [Header("상점 정보")]
    public bool hasShop = true;
    public List<ItemData> shopInventory = new List<ItemData>();

    [Header("이벤트 연결 (옵션)")]
    public QuestItemActive questItemActivator;
    public QuestBoss questBoss;

    private Transform _playerTransform;

    [Header("UI 연결")]
    public GameObject ShopPanel;
    public DialogueUIView dialogueUI;

    // 현재 UI가 열려있는지 여부
    private bool _isUIOpen = false;

    // =========================
    // 대화 세트
    // =========================
    [System.Serializable]
    public class DialogueSet
    {
        [TextArea(1, 3)]
        public List<string> dialogues = new List<string>();
    }

    [Header("대사 목록")]
    public DialogueSet startQuestDialogue;
    public DialogueSet inProgressDialogue;
    public DialogueSet completeQuestDialogue;
    public DialogueSet completedDialogue;
    public DialogueSet shopOnlyDialogue;

    [Header("NPC 목소리 설정")]
    public List<AudioClip> npcVoices;

    [Header("퀘스트 아이콘 설정")]
    public GameObject exclamationMark;
    public GameObject questionMark;
    public Vector3 iconOffset = new Vector3(0, 2.5f, 0);

    private GameObject _currentIcon;
    private MonoBehaviour _playerMovement;

    // =========================
    // 유니티 생명주기
    // =========================
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _playerMovement = playerObj.GetComponent<MonoBehaviour>();
        }

        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUIView>();
        }

        UpdateQuestIcon();
    }

    // ★ [수정] 에디터 상에서 상호작용 범위(시작/종료) 시각화
    private void OnDrawGizmosSelected()
    {
        // 1. 상호작용 시작 범위 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // 2. 상호작용 종료 범위 (빨간색, 약간 투명)
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _exitInteractionRange);
    }

    public void UpdateQuestIcon()
    {
        if (_currentIcon != null) Destroy(_currentIcon);

        GameObject prefabToSpawn = null;

        switch (currentQuestState)
        {
            case QuestState.NOT_STARTED:
                if (availableQuest != null && availableQuest.questID != 0)
                    prefabToSpawn = exclamationMark;
                break;
            case QuestState.IN_PROGRESS:
            case QuestState.CAN_BE_COMPLETED:
                prefabToSpawn = questionMark;
                break;
        }

        if (prefabToSpawn != null)
        {
            _currentIcon = Instantiate(prefabToSpawn, transform.position + iconOffset, Quaternion.identity, transform);
        }
    }

    void Update()
    {
        if (_playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        // 1. 상호작용 시작 가능 거리 (가까울 때)
        if (distanceToPlayer <= interactionRange)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                // 1-1. 대화창이 떠있으면 대화 넘기기 (최우선)
                if (dialogueUI != null && dialogueUI.IsDialogueOpen())
                {
                    dialogueUI.HandleNextMessage(npcName);
                    return;
                }

                // 1-2. 상점이 켜져있으면 닫기
                if (ShopPanel != null && ShopPanel.activeSelf)
                {
                    CloseAllNPCUI();
                    return;
                }

                // 1-3. 아무것도 없으면 상호작용 시작
                InteractWithPlayer();
            }
        }
        // 2. 상호작용 강제 종료 거리 (멀어졌을 때)
        // ★ [핵심] interactionRange가 아니라 _exitInteractionRange(여유 범위)를 사용
        else if (_isUIOpen && distanceToPlayer > _exitInteractionRange)
        {
            CloseAllNPCUI();
        }
    }

    void SetPlayerControl(bool state)
    {
        if (_playerMovement != null) _playerMovement.enabled = state;
    }

    void InteractWithPlayer()
    {
        // 상호작용 시작 시 현재 NPC 등록
        ActiveNPC = this;
        _isUIOpen = true; // UI 열림 상태 즉시 설정

        CheckQuestItemCount();

        if (availableQuest != null && availableQuest.questID != 0)
        {
            List<string> messages = null;
            Action postDialogueAction = null;

            switch (currentQuestState)
            {
                case QuestState.NOT_STARTED:
                    messages = startQuestDialogue.dialogues;
                    postDialogueAction = () => { dialogueUI.ShowActionButtons(AcceptQuest, RefuseQuest); };
                    break;

                case QuestState.IN_PROGRESS:
                    messages = inProgressDialogue.dialogues;
                    postDialogueAction = () => { CloseAllNPCUI(); };
                    break;

                case QuestState.CAN_BE_COMPLETED:
                    messages = completeQuestDialogue.dialogues;
                    postDialogueAction = CompleteQuest;
                    break;

                case QuestState.COMPLETED:
                    messages = completedDialogue.dialogues;
                    if (hasShop) postDialogueAction = () => { ShowPanel(ShopPanel); };
                    else postDialogueAction = () => { CloseAllNPCUI(); };
                    break;
            }

            if (messages == null || messages.Count == 0)
            {
                messages = new List<string> { "..." };
            }

            ShowGenericDialogue(npcName, messages, postDialogueAction);
        }
        else
        {
            List<string> messages = shopOnlyDialogue.dialogues;
            if (messages == null || messages.Count == 0) messages = startQuestDialogue.dialogues;

            Action onComplete = () =>
            {
                if (questItemActivator != null) questItemActivator.ActivateItems();
                if (questBoss != null) questBoss.SpawnBoss();

                if (hasShop) ShowPanel(ShopPanel);
                else CloseAllNPCUI();
            };

            ShowGenericDialogue(npcName, messages, onComplete);
        }
    }

    private void CheckQuestItemCount()
    {
        if (currentQuestState == QuestState.IN_PROGRESS && availableQuest.goalItem != null)
        {
            Inventory playerInventory = FindObjectOfType<Inventory>();

            if (playerInventory != null)
            {
                int count = 0;
                foreach (var item in playerInventory.Items)
                {
                    if (item != null && item.itemName == availableQuest.goalItem.itemName)
                    {
                        count++;
                    }
                }

                if (count >= availableQuest.requiredAmount)
                {
                    currentQuestState = QuestState.CAN_BE_COMPLETED;
                    UpdateQuestIcon();
                }
            }
        }
    }

    void ShowPanel(GameObject panelToShow)
    {
        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            _isUIOpen = true;
            SetPlayerControl(false);

            // 상점을 열 때 시간을 멈춤
            //Time.timeScale = 0f;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // 상점 피드백 대화
    public void ShowShopFeedback(string message)
    {
        if (dialogueUI != null)
        {
            // 1. 대화창 애니메이션과 타이핑 효과를 위해 시간을 다시 흐르게 함
            Time.timeScale = 1f;

            _isUIOpen = true;
            SetPlayerControl(false);

            List<string> msgList = new List<string> { message };

            // 2. 대화가 끝났을 때 실행할 콜백 정의
            Action onFeedbackComplete = () =>
            {
                // 상점 패널이 여전히 켜져 있다면 (구매 성공/실패 메시지)
                if (ShopPanel != null && ShopPanel.activeSelf)
                {
                    // 대화창이 닫히는 애니메이션을 기다린 후 시간을 멈춤
                    StartCoroutine(FreezeTimeAfterDelay(0.5f));
                }
                else
                {
                    // 상점 패널이 꺼져 있다면 (작별 인사)
                    CloseAllNPCUI();
                }
            };

            dialogueUI.ShowDialogueList(npcName, msgList, onFeedbackComplete, npcVoices);
        }
    }

    // 애니메이션을 기다렸다가 시간을 멈추는 코루틴
    private IEnumerator FreezeTimeAfterDelay(float delay)
    {
        // 대화창이 닫히는 애니메이션 동안 대기 (Realtime 사용으로 TimeScale 영향 안 받음)
        yield return new WaitForSecondsRealtime(delay);

        // 대기 후에도 상점이 열려있다면 시간 정지
        if (ShopPanel != null && ShopPanel.activeSelf)
        {
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void AcceptQuest()
    {
        currentQuestState = QuestState.IN_PROGRESS;
        UpdateQuestIcon();
        if (QuestHUDView.Instance != null)
        {
            QuestHUDView.Instance.UpdateQuestHUD(
                availableQuest.questID,
                availableQuest.questName,
                availableQuest.goalItem != null ? availableQuest.goalItem.itemName : "???",
                0,
                availableQuest.requiredAmount
            );
        }
        CloseAllNPCUI();
    }

    public void RefuseQuest()
    {
        CloseAllNPCUI();
    }

    void CompleteQuest()
    {
        Inventory playerInventory = FindObjectOfType<Inventory>();
        if (playerInventory != null && availableQuest.goalItem != null)
        {
            for (int i = 0; i < availableQuest.requiredAmount; i++)
            {
                playerInventory.RemoveItem(availableQuest.goalItem);
            }
        }

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoin(availableQuest.rewardCoin);
        }

        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.GainExp(availableQuest.rewardExp);
        }

        currentQuestState = QuestState.COMPLETED;

        if (QuestHUDView.Instance != null)
        {
            QuestHUDView.Instance.RemoveQuestHUD(availableQuest.questID);
        }

        if (QuestManager.Instance != null)
        {
            switch (availableQuest.questID)
            {
                case 101: QuestManager.Instance.isSundaeDone = true; break;
                case 102: QuestManager.Instance.isFriedFoodDone = true; break;
                case 103: QuestManager.Instance.isTteokbokkiDone = true; break;
            }
            QuestManager.Instance.CheckAndShowTeacher();
        }

        UpdateQuestIcon();
        CloseAllNPCUI();
    }

    void ShowGenericDialogue(string name, List<string> messages, Action onAllHideComplete)
    {
        if (dialogueUI != null)
        {
            SetPlayerControl(false);
            _isUIOpen = true;
            List<string> messagesCopy = new List<string>(messages);
            dialogueUI.ShowDialogueList(name, messagesCopy, onAllHideComplete, npcVoices);
        }
    }

    public void CloseAllNPCUI()
    {
        // 상호작용 종료 시 ActiveNPC 해제
        if (ActiveNPC == this) ActiveNPC = null;

        if (dialogueUI != null && dialogueUI.IsDialogueOpen())
        {
            dialogueUI.HideDialogue();
        }
        if (ShopPanel != null) ShopPanel.SetActive(false);

        _isUIOpen = false;
        SetPlayerControl(true);

        // 모든 UI가 닫힐 때 시간 정상화
        Time.timeScale = 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void BuyItem(ItemData itemToBuy, int price)
    {
        if (CoinManager.Instance == null) return;
        if (CoinManager.Instance.GetCurrentCoin() >= price)
        {
            CoinManager.Instance.AddCoin(-price);

            Inventory playerInventory = FindObjectOfType<Inventory>();
            if (playerInventory != null)
            {
                playerInventory.AddItem(itemToBuy);
                Debug.Log($"{itemToBuy.itemName} 구매 완료!");
            }
        }
        else
        {
            Debug.Log("코인이 부족합니다.");
        }
    }

    public bool IsDialogueActive()
    {
        return _isUIOpen || (ShopPanel != null && ShopPanel.activeSelf);
    }
}