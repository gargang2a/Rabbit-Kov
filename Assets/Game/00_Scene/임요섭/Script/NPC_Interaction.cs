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

    [Header("--- 이벤트 설정 (무기 지급) ---")]
    public List<ItemData> starterWeapons;
    private bool hasGivenStarterItems = false;

    [Header("--- 퀘스트 아이콘 설정 ---")]
    public GameObject exclamationMark;
    public GameObject questionMark;
    public Vector3 iconOffset = new Vector3(0, 2.5f, 0);

    private GameObject currentIcon;
    private MonoBehaviour playerMovement;
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
        if (currentIcon != null)
        {
            Destroy(currentIcon);
        }

        GameObject prefabToSpawn = null;

        switch (currentQuestState)
        {
            case QuestState.NOT_STARTED:
                prefabToSpawn = exclamationMark;
                break;
            case QuestState.IN_PROGRESS:
            case QuestState.CAN_BE_COMPLETED:
                prefabToSpawn = questionMark;
                break;
            case QuestState.COMPLETED:
                prefabToSpawn = null;
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
                if (dialogueUI != null && dialogueUI.IsDialogueOpen())
                {
                    dialogueUI.HandleNextMessage(npcName);
                }
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
    void SetPlayerControl(bool state)
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = state;
        }
    }
    void InteractWithPlayer()
    {
        CheckQuestItemCount();
        isUIOpen = false;
        if (ShopPanel != null && ShopPanel.activeSelf)
        {
            CloseAllNPCUI();
            return;
        }
        isUIOpen = false;

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
            List<string> messages = startQuestDialogue.dialogues;

            Action onComplete = () =>
            {
                GiveStarterItems();
                if (hasShop)
                {
                    ShowPanel(ShopPanel);
                }
                else
                {
                    CloseAllNPCUI();
                }
            };
            isUIOpen = true;
            ShowGenericDialogue(npcName, messages, onComplete);
        }
    }
    private void GiveStarterItems()
    {
        if (hasGivenStarterItems) return;

        Inventory playerInventory = FindObjectOfType<Inventory>();
        if (playerInventory != null && starterWeapons != null)
        {
            foreach (ItemData item in starterWeapons)
            {
                playerInventory.AddItem(item);
            }
            hasGivenStarterItems = true;
            Debug.Log("기본 무기(Pistol, Knife) 지급 완료");
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
        currentQuestState = QuestState.COMPLETED;
        UpdateQuestIcon();
        CloseAllNPCUI();
    }
    void ShowShopUI() { }
    void ShowGenericDialogue(string name, List<string> messages, Action onAllHideComplete)
    {
        if (dialogueUI != null)
        {
            SetPlayerControl(false);
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
                Debug.Log($"{itemToBuy.itemName} 구매 완료! 잔액: {CoinManager.Instance.GetCurrentCoin()}");
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