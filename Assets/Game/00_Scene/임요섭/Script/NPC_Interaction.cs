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

    // ★ [추가] 대화 종료 후 아이템을 활성화할 스크립트 연결
    [Header("이벤트 연결 (옵션)")]
    public QuestItemActive questItemActivator;

    private Transform playerTransform;

    [Header("UI 연결")]
    public GameObject ShopPanel;
    public DialogueUIView dialogueUI;

    // 현재 UI가 열려있는지 여부
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
            playerMovement = playerObj.GetComponent<MonoBehaviour>();
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
            currentIcon = Instantiate(prefabToSpawn, transform.position + iconOffset, Quaternion.identity, transform);
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= interactionRange)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                if (ShopPanel != null && ShopPanel.activeSelf)
                {
                    CloseAllNPCUI();
                    return;
                }

                if (dialogueUI != null && dialogueUI.IsDialogueOpen())
                {
                    dialogueUI.HandleNextMessage(npcName);
                    return;
                }

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
        CheckQuestItemCount();
        isUIOpen = false;

        // 퀘스트가 있는 NPC인 경우
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
            // 퀘스트가 없는 NPC (튜토리얼 NPC 등)
            List<string> messages = shopOnlyDialogue.dialogues;
            if (messages == null || messages.Count == 0) messages = startQuestDialogue.dialogues;

            // 대화가 끝난 후 실행될 로직
            Action onComplete = () =>
            {
                // ★ [추가] 대화 종료 시 아이템 활성화 요청
                if (questItemActivator != null)
                {
                    questItemActivator.ActivateItems();
                }

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
            for (int i = 0; i < availableQuest.requiredAmount; i++)
            {
                playerInventory.RemoveItem(availableQuest.goalItem);
            }
        }

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoin(availableQuest.rewardCoin);
        }

        // 경험치 지급
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
            isUIOpen = true;
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