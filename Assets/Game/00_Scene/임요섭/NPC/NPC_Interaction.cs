using System;
using System.Collections.Generic;
using UnityEngine;
public class NPC_Interaction : MonoBehaviour
{
    public enum QuestState
    {
        NOT_STARTED,
        IN_PROGRESS,
        CAN_BE_COMPLETED,
        COMPLETED
    }
    [System.Serializable]
    public class ShopItem
    {
        public string itemName;
        public int itemID;
        public int price;
        public int requiredQuestID = -1;
    }
    [System.Serializable]
    public class QuestDetails
    {
        [Header("Quest Info")]
        public int questID;
        public string questName;
        [TextArea(3, 5)]
        public string description;

        [Header("Goal")]
        public string goalItemName;
        public int requiredAmount;

        [Header("Reward")]
        public int rewardGold;
        public int rewardExp;
        public ShopItem rewardItem;
    }

    [Header("--- NPC 설정 ---")]
    public string npcName = "Jason";
    public KeyCode interactionKey = KeyCode.F;
    public float interactionRange = 3f;

    [Header("--- 퀘스트 정보 ---")]
    public QuestDetails availableQuest;
    public QuestState currentQuestState = QuestState.NOT_STARTED;

    [Header("--- 상점 정보 ---")]
    public bool hasShop = true;
    public List<ShopItem> shopInventory = new List<ShopItem>();

    private Transform playerTransform;

    [Header("--- UI 연결---")]
    public GameObject ShopPanel;

    [Header("--- 애니메이션 연결 ---")]
    public DialogueUIView dialogueUI;

    private bool isUIOpen = false;
    [System.Serializable]
    public class DialogueSet
    {
        [Header(" 대화목록")]
        [TextArea(1, 3)]
        public List<string> dialogues = new List<string>();
    }
    [Header("--- 대사 목록 ---")]
    public DialogueSet startQuestDialogue;
    public DialogueSet inProgressDialogue;
    public DialogueSet completeQuestDialogue;
    public DialogueSet completedDialogue;
    public DialogueSet shopOnlyDialogue;
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUIView>();
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
                if (dialogueUI != null && dialogueUI.IsDialogueOpen())
                {
                    dialogueUI.HandleNextMessage(npcName);
                }
                else if (!isUIOpen)
                {
                    InteractWithPlayer();
                }
            }
        }
        else if (isUIOpen && distanceToPlayer > interactionRange)
        {
            CloseAllNPCUI();
        }
    }
    void InteractWithPlayer()
    {
        if (isUIOpen)
        {
            if (dialogueUI != null && dialogueUI.IsDialogueOpen())
            {
                dialogueUI.HandleNextMessage(npcName);
            }
            else if (ShopPanel != null && ShopPanel.activeSelf)
            {
                CloseAllNPCUI();
            }
            return;
        }

        if (availableQuest != null)
        {
            List<string> messages = null;
            Action postDialogueAction = null;
            switch (currentQuestState)
            {
                case QuestState.NOT_STARTED:
                    messages = startQuestDialogue.dialogues;
                    postDialogueAction = () =>
                    {
                        dialogueUI.ShowActionButtons(AcceptQuest, RefuseQuest);
                    };
                    break;
                case QuestState.IN_PROGRESS:
                    messages = inProgressDialogue.dialogues;
                    postDialogueAction = CheckQuestCompletion;
                    break;
                case QuestState.CAN_BE_COMPLETED:
                    messages = completeQuestDialogue.dialogues;
                    postDialogueAction = CompleteQuest;
                    break;
                case QuestState.COMPLETED:
                    messages = completedDialogue.dialogues;
                    if (hasShop)
                    {
                        postDialogueAction = () => { ShowPanel(ShopPanel); };
                    }
                    break;
            }
            if (messages == null || messages.Count == 0)
            {
                messages = new List<string> { "..." };
                postDialogueAction = null;
            }
            ShowGenericDialogue(npcName, messages, postDialogueAction);
        }
        else if (hasShop)
        {
            List<string> messages = shopOnlyDialogue.dialogues;
            Action postDialogueAction = () => { ShowPanel(ShopPanel); };
            ShowGenericDialogue(npcName, messages, postDialogueAction);
        }
        else
        {
            List<string> defaultMessage = new List<string> { "특별히 드릴 말씀이 없어요." };
            ShowGenericDialogue(npcName, defaultMessage, null);
        }
    }
    void ShowPanel(GameObject panelToShow)
    {
        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            isUIOpen = true;
        }
    }
    public void AcceptQuest()
    {
        currentQuestState = QuestState.IN_PROGRESS;
        Debug.Log($"퀘스트 '{availableQuest.questName}'를 수락했습니다. 목표: {availableQuest.goalItemName} {availableQuest.requiredAmount}개");
    }
    public void RefuseQuest()
    {
        Debug.Log($"퀘스트 '{availableQuest.questName}'를 거절했습니다. 다음 상호작용 시 대화가 재개됩니다.");
    }
    void CheckQuestCompletion() { }
    void CompleteQuest()
    {
        Debug.Log($"퀘스트 완료! 보상 지급: {availableQuest.rewardGold} 골드, {availableQuest.rewardExp} 경험치.");
        currentQuestState = QuestState.COMPLETED;
        CloseAllNPCUI();
    }
    void ShowShopUI() { }
    void ShowGenericDialogue(string name, List<string> messages, Action onAllHideComplete)
    {
        if (ShopPanel != null) ShopPanel.SetActive(false);

        if (dialogueUI != null)
        {
            dialogueUI.ShowDialogueList(name, messages, onAllHideComplete);
            isUIOpen = true;
        }
    }
    public void CloseAllNPCUI()
    {
        if (!isUIOpen) return;

        if (dialogueUI != null && dialogueUI.IsDialogueOpen())
        {
            dialogueUI.HideDialogue();
        }

        if (ShopPanel != null) ShopPanel.SetActive(false);

        isUIOpen = false;
    }
}