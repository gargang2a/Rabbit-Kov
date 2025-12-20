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
        public ItemData itemData;
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
        public ItemData goalItem;
        public int requiredAmount;

        [Header("Reward")]
        public int rewardCoin;
        public int rewardExp;
        public ItemData rewardItem;
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
    public List<ItemData> shopInventory = new List<ItemData>();

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

    [Header("--- NPC 목소리 설정 ---")]
    public List<AudioClip> npcVoices;

    private MonoBehaviour playerMovement;
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
                // 1. 대화창 UI가 실제로 켜져 있다면 다음 메시지 처리
                if (dialogueUI != null && dialogueUI.IsDialogueOpen())
                {
                    dialogueUI.HandleNextMessage(npcName);
                }
                // 2. 대화창이 닫혀 있다면 대화 새로 시작
                else
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
        CheckQuestItemCount();
        isUIOpen = false;
        // 이미 상점이 열려있거나 하는 예외 상황 처리
        if (ShopPanel != null && ShopPanel.activeSelf)
        {
            CloseAllNPCUI();
            return;
        }

        // 대화를 새로 시작할 것이므로 초기화
        isUIOpen = false;

        if (availableQuest != null)
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
            isUIOpen = true;
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
    private void CheckQuestItemCount()
    {
        if (currentQuestState == QuestState.IN_PROGRESS)
        {
            Inventory playerInventory = FindObjectOfType<Inventory>();

            if (playerInventory != null)
            {
                int count = 0;
                foreach (var item in playerInventory.Items)
                {
                    if (item == availableQuest.goalItem) count++;
                }
                if (count >= availableQuest.requiredAmount)
                {
                    currentQuestState = QuestState.CAN_BE_COMPLETED;
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
            Time.timeScale = 0f;          // 1. 게임 시간 정지
            Cursor.visible = true;         // 2. 마우스 커서 보이기
            Cursor.lockState = CursorLockMode.None; // 3. 마우스 고정 해제
        }
    }
    // AcceptQuest 수정
    public void AcceptQuest()
    {
        currentQuestState = QuestState.IN_PROGRESS;

        if (QuestHUDView.Instance != null)
        {
            // [수정] goalItem 객체에서 이름을 가져와 전달합니다.
            QuestHUDView.Instance.UpdateQuestHUD(
                availableQuest.questID,
                availableQuest.questName,
                availableQuest.goalItem != null ? availableQuest.goalItem.itemName : "알 수 없는 아이템",
                0,
                availableQuest.requiredAmount
            );
        }
        CloseAllNPCUI();
    }
    public void RefuseQuest()
    {
        Debug.Log($"퀘스트 거절");
        CloseAllNPCUI();
    }
    void CheckQuestCompletion() { }
    void CompleteQuest()
    {
        Inventory playerInventory = FindObjectOfType<Inventory>();
        if (playerInventory != null)
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
        currentQuestState = QuestState.COMPLETED;
        if (QuestHUDView.Instance != null)
        {
            QuestHUDView.Instance.RemoveQuestHUD(availableQuest.questID);
        }

        if (QuestManager.Instance != null)
        {
            switch (availableQuest.questID)
            {
                case 101: QuestManager.Instance.isFriedFoodDone = true; break;
                case 102: QuestManager.Instance.isSundaeDone = true; break;
                case 103: QuestManager.Instance.isTteokbokkiDone = true; break;
            }
            QuestManager.Instance.CheckAndShowTeacher();
        }
        CloseAllNPCUI();
    }
    void ShowShopUI() { }
    void ShowGenericDialogue(string name, List<string> messages, Action onAllHideComplete)
    {
        if (dialogueUI != null)
        {
            dialogueUI.ShowDialogueList(name, messages, onAllHideComplete, npcVoices);
            isUIOpen = true;
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
        Time.timeScale = 1f;           // 1. 게임 시간 재개
        Cursor.visible = false;        // 2. 마우스 커서 숨기기
        Cursor.lockState = CursorLockMode.Locked; // 3. 마우스 다시 고정
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
                Debug.Log($"{itemToBuy.itemName} 구매 완료! 잔액: {CoinManager.Instance.GetCurrentCoin()}");
            }
        }
        else
        {
            Debug.Log("코인이 부족합니다.");
        }
    }
}