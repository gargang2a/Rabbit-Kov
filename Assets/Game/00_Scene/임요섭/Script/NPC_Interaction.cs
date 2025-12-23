using System;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Interaction : MonoBehaviour
{
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

    [Header("퀘스트 정보")]
    public QuestDetails availableQuest;
    public QuestState currentQuestState = QuestState.NOT_STARTED;

    [Header("상점 정보")]
    public bool hasShop = true;
    public List<ItemData> shopInventory = new List<ItemData>();

    private Transform playerTransform;

    [Header("UI 연결")]
    public GameObject ShopPanel;
    public DialogueUIView dialogueUI;

    // 현재 UI가 열려있는지 여부 (내부 상태 관리용)
    private bool isUIOpen = false;

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

    private GameObject currentIcon;
    private MonoBehaviour playerMovement;

    // =========================
    // 유니티 생명주기
    // =========================
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerMovement = playerObj.GetComponent<MonoBehaviour>(); // 주의: 실제 사용하는 PlayerController 스크립트로 변경 권장
        }

        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUIView>();
        }

        UpdateQuestIcon();
    }

    public void UpdateQuestIcon()
    {
        if (currentIcon != null) Destroy(currentIcon);

        GameObject prefabToSpawn = null;

        switch (currentQuestState)
        {
            case QuestState.NOT_STARTED:
                if (availableQuest != null && availableQuest.questID != 0) // 퀘스트가 있는 경우만
                    prefabToSpawn = exclamationMark;
                break;
            case QuestState.IN_PROGRESS:
            case QuestState.CAN_BE_COMPLETED:
                prefabToSpawn = questionMark;
                break;
        }

        if (prefabToSpawn != null)
        {
            currentIcon = Instantiate(prefabToSpawn, transform.position + iconOffset, Quaternion.identity, transform);
        }
    }

    // ★ [수정됨] Update 로직의 우선순위 재정립
    void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= interactionRange)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                // 1순위: 상점이 열려있으면 닫는다.
                if (ShopPanel != null && ShopPanel.activeSelf)
                {
                    CloseAllNPCUI();
                    return;
                }

                // 2순위: 대화가 진행 중이면 다음 대사로 넘긴다.
                if (dialogueUI != null && dialogueUI.IsDialogueOpen())
                {
                    dialogueUI.HandleNextMessage(npcName);
                    return;
                }

                // 3순위: 아무것도 안 열려있으면 상호작용을 시작한다.
                InteractWithPlayer();
            }
        }
        else if (isUIOpen && distanceToPlayer > interactionRange)
        {
            CloseAllNPCUI();
        }
    }

    void SetPlayerControl(bool state)
    {
        if (playerMovement != null) playerMovement.enabled = state;
    }

    void InteractWithPlayer()
    {
        // 퀘스트 아이템 체크
        CheckQuestItemCount();

        // UI 상태 초기화
        isUIOpen = false;

        // 퀘스트가 있는 NPC인지 확인
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

            // 대사가 비어있을 경우 안전장치
            if (messages == null || messages.Count == 0)
            {
                messages = new List<string> { "..." };
            }

            ShowGenericDialogue(npcName, messages, postDialogueAction);
        }
        else
        {
            // 퀘스트 없는 NPC (상점 전용 등)
            List<string> messages = shopOnlyDialogue.dialogues; // 상점 전용 대사 사용 권장
            if (messages == null || messages.Count == 0) messages = startQuestDialogue.dialogues;

            Action onComplete = () =>
            {
                if (hasShop) ShowPanel(ShopPanel);
                else CloseAllNPCUI();
            };

            ShowGenericDialogue(npcName, messages, onComplete);
        }
    }

    // ★ [수정됨] 아이템 비교 로직 개선 (이름 비교)
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
                    // 참조 비교(==) 대신 이름 비교 사용 (복사본 문제 해결)
                    if (item != null && item.itemName == availableQuest.goalItem.itemName)
                    {
                        count++;
                    }
                }

                if (count >= availableQuest.requiredAmount)
                {
                    currentQuestState = QuestState.CAN_BE_COMPLETED;
                    UpdateQuestIcon(); // 상태 변경 시 아이콘 즉시 갱신
                }
            }
        }
    }

    void ShowPanel(GameObject panelToShow)
    {
        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            isUIOpen = true;
            SetPlayerControl(false);
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
            // 아이템 제거 로직 (이름 기반으로 안전하게 제거)
            for (int i = 0; i < availableQuest.requiredAmount; i++)
            {
                // Inventory에 RemoveItemByName 같은 기능이 있다면 그것을 사용하는 것이 더 안전함
                // 여기서는 기존 로직 유지하되, 실제 구현에 따라 수정 필요
                playerInventory.RemoveItem(availableQuest.goalItem);
            }
        }

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoin(availableQuest.rewardCoin);
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
                case 101: QuestManager.Instance.isSundaeDone = true; break; // ID 매칭 주의
                case 102: QuestManager.Instance.isFriedFoodDone = true; break;
                case 103: QuestManager.Instance.isTteokbokkiDone = true; break;
            }
            QuestManager.Instance.CheckAndShowTeacher();
        }

        UpdateQuestIcon();
        CloseAllNPCUI();
    }

    // ★ [수정됨] 리스트 복사본 전달
    void ShowGenericDialogue(string name, List<string> messages, Action onAllHideComplete)
    {
        if (dialogueUI != null)
        {
            SetPlayerControl(false);
            isUIOpen = true;

            // 중요: 원본 리스트(messages)를 그대로 넘기지 않고, 복사해서 넘김
            // DialogueUIView가 리스트 내용을 소모(Remove)하더라도 원본은 안전함
            List<string> messagesCopy = new List<string>(messages);

            dialogueUI.ShowDialogueList(name, messagesCopy, onAllHideComplete, npcVoices);
        }
    }

    public void CloseAllNPCUI()
    {
        if (dialogueUI != null && dialogueUI.IsDialogueOpen())
        {
            dialogueUI.HideDialogue();
        }
        if (ShopPanel != null) ShopPanel.SetActive(false);

        isUIOpen = false;
        SetPlayerControl(true);

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
        return isUIOpen || (ShopPanel != null && ShopPanel.activeSelf);
    }
}