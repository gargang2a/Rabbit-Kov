using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Interaction : MonoBehaviour
{
    public static NPC_Interaction ActiveNPC;

    public enum QuestState
    {
        NOT_STARTED,
        IN_PROGRESS,
        CAN_BE_COMPLETED,
        COMPLETED
    }

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

    [Header("NPC 설정")]
    public string npcName = "";
    public KeyCode interactionKey = KeyCode.F;
    public float interactionRange = 3f;
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

    private bool _isUIOpen = false;

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

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
            // 진행 중일 때도 완료 조건이 충족되면 물음표가 떠야 하므로 로직 분리
            case QuestState.IN_PROGRESS:
                // 진행 중일 때는 아이콘 없음 (혹은 회색 물음표 등 기획에 따라 추가 가능)
                break;
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
        // ★ [추가됨] 실시간으로 퀘스트 아이템 보유량을 체크하여 아이콘(물음표)을 갱신
        // 이렇게 해야 아이템을 먹자마자 NPC 머리 위에 물음표가 뜸
        if (currentQuestState == QuestState.IN_PROGRESS || currentQuestState == QuestState.CAN_BE_COMPLETED)
        {
            CheckQuestItemCount();
        }

        if (_playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer <= interactionRange)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                if (dialogueUI != null && dialogueUI.IsDialogueOpen())
                {
                    dialogueUI.HandleNextMessage(npcName);
                    return;
                }

                if (ShopPanel != null && ShopPanel.activeSelf)
                {
                    CloseAllNPCUI();
                    return;
                }

                InteractWithPlayer();
            }
        }
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
        ActiveNPC = this;
        _isUIOpen = true;

        // 상호작용 시 한 번 더 체크 (안전장치)
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
                    // ★ [중요] 여기서만 CompleteQuest가 연결됨. 즉, 대화를 해야만 완료됨.
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

    // ★ [수정됨] 아이템 개수 체크 로직 개선
    private void CheckQuestItemCount()
    {
        // 퀘스트가 진행 중이거나 완료 가능 상태일 때만 체크
        if (availableQuest.goalItem != null &&
           (currentQuestState == QuestState.IN_PROGRESS || currentQuestState == QuestState.CAN_BE_COMPLETED))
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

                // 상태 변화 감지
                QuestState previousState = currentQuestState;

                if (count >= availableQuest.requiredAmount)
                {
                    currentQuestState = QuestState.CAN_BE_COMPLETED;
                }
                else
                {
                    // 아이템을 버렸을 경우 다시 진행 중으로 변경
                    currentQuestState = QuestState.IN_PROGRESS;
                }

                // 상태가 변했을 때만 아이콘 업데이트 (성능 최적화)
                if (previousState != currentQuestState)
                {
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
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void ShowShopFeedback(string message)
    {
        if (dialogueUI != null)
        {
            Time.timeScale = 1f;
            _isUIOpen = true;
            SetPlayerControl(false);

            List<string> msgList = new List<string> { message };

            Action onFeedbackComplete = () =>
            {
                if (ShopPanel != null && ShopPanel.activeSelf)
                {
                    StartCoroutine(FreezeTimeAfterDelay(0.5f));
                }
                else
                {
                    CloseAllNPCUI();
                }
            };

            dialogueUI.ShowDialogueList(npcName, msgList, onFeedbackComplete, npcVoices);
        }
    }

    private IEnumerator FreezeTimeAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

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
        Debug.Log($"[NPC] {npcName}: 퀘스트 완료 로직 시작"); // 디버깅용 로그

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

        if (playerInventory != null && availableQuest.rewardItem != null)
        {
            playerInventory.AddItem(availableQuest.rewardItem);
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
        if (ActiveNPC == this) ActiveNPC = null;

        if (dialogueUI != null && dialogueUI.IsDialogueOpen())
        {
            dialogueUI.HideDialogue();
        }
        if (ShopPanel != null) ShopPanel.SetActive(false);

        _isUIOpen = false;
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
        return _isUIOpen || (ShopPanel != null && ShopPanel.activeSelf);
    }
}