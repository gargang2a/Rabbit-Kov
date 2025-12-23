using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC와 플레이어의 상호작용을 처리하는 컴포넌트입니다.
/// - 대화, 퀘스트 시작/완료, 상점 열기 등을 담당합니다.
/// </summary>
public class NPC_Interaction : MonoBehaviour
{
    // =========================
    // 퀘스트 상태 열거형
    // =========================
    /// <summary>퀘스트 진행 상태</summary>
    public enum QuestState
    {
        NOT_STARTED,
        IN_PROGRESS,
        CAN_BE_COMPLETED,
        COMPLETED
    }

    // =========================
    // 상점용 데이터 구조체
    // =========================
    [System.Serializable]
    public class ShopItem
    {
        [Tooltip("상점에 판매되는 아이템 데이터")]
        public ItemData itemData;

        [Tooltip("아이템 가격")]
        public int price;

        [Tooltip("이 아이템을 보기 위해 필요한 퀘스트 ID (없으면 -1)")]
        public int requiredQuestID = -1;
    }

    // =========================
    // 퀘스트 상세 정보
    // =========================
    [System.Serializable]
    public class QuestDetails
    {
        [Header("퀘스트 정보")]
        [Tooltip("퀘스트 고유 ID")]
        public int questID;

        [Tooltip("퀘스트 이름")]
        public string questName;

        [Tooltip("퀘스트 설명 (Inspector에서 여러 줄 가능)")]
        [TextArea(0, 15)]
        public string description;

        [Header("목표")]
        [Tooltip("퀘스트 목표 아이템")]
        public ItemData goalItem;

        [Tooltip("필요 수량")]
        public int requiredAmount;

        [Header("보상")]
        [Tooltip("보상 코인")]
        public int rewardCoin;

        [Tooltip("보상 경험치")]
        public int rewardExp;

        [Tooltip("보상 아이템 (있으면 지급)")]
        public ItemData rewardItem;
    }

    // =========================
    // NPC 기본 설정 (인스펙터 노출 필드)
    // =========================
    [Header("NPC 설정")]
    [Tooltip("NPC 표시 이름")]
    public string npcName = "";

    [Tooltip("플레이어와 상호작용할 키 (기본: F)")]
    public KeyCode interactionKey = KeyCode.F;

    [Tooltip("플레이어와 상호작용 가능한 거리")]
    public float interactionRange = 3f;

    [Header("퀘스트 정보")]
    [Tooltip("해당 NPC가 제공하는 퀘스트 상세 정보")]
    public QuestDetails availableQuest;

    [Tooltip("현재 퀘스트 상태")]
    public QuestState currentQuestState = QuestState.NOT_STARTED;

    [Header("상점 정보")]
    [Tooltip("이 NPC가 상점을 가지고 있는지 여부")]
    public bool hasShop = true;

    [Tooltip("상점 인벤토리 (Inspector에서 아이템 추가)")]
    public List<ItemData> shopInventory = new List<ItemData>();

    // 플레이어 Transform 캐시
    private Transform playerTransform;

    [Header("UI 연결")]
    [Tooltip("상점 패널 GameObject (Inspector에 연결)")]
    public GameObject ShopPanel;

    [Header("대화 UI 연결")]
    [Tooltip("대화 UI 컴포넌트 (DialogueUIView)")]
    public DialogueUIView dialogueUI;

    // 현재 UI가 열려있는지 여부
    private bool isUIOpen = false;

    // =========================
    // 대화 세트 (인스펙터에서 편집)
    // =========================
    [System.Serializable]
    public class DialogueSet
    {
        [Header("대화 목록")]
        [Tooltip("대사 목록 (Inspector에서 줄바꿈 허용)")]
        [TextArea(1, 3)]
        public List<string> dialogues = new List<string>();
    }

    [Header("대사 목록")]
    [Tooltip("퀘스트 시작 시 대사")]
    public DialogueSet startQuestDialogue;

    [Tooltip("진행 중일 때의 대사")]
    public DialogueSet inProgressDialogue;

    [Tooltip("완료 가능 상태일 때의 대사")]
    public DialogueSet completeQuestDialogue;

    [Tooltip("퀘스트 완료 후 대사")]
    public DialogueSet completedDialogue;

    [Tooltip("상점 전용 대사 (퀘스트 관련이 없을 때)")]
    public DialogueSet shopOnlyDialogue;

    [Header("NPC 목소리 설정")]
    [Tooltip("대사 재생에 사용할 음성(클립) 리스트")]
    public List<AudioClip> npcVoices;

    [Header("퀘스트 아이콘 설정")]
    [Tooltip("퀘스트 시작 아이콘 (느낌표 프리팹)")]
    public GameObject exclamationMark;

    [Tooltip("퀘스트 진행/완료 가능 아이콘 (물음표 프리팹)")]
    public GameObject questionMark;

    [Tooltip("아이콘이 NPC 머리 위에 위치할 오프셋")]
    public Vector3 iconOffset = new Vector3(0, 2.5f, 0);

    // 내부 사용 변수
    private GameObject currentIcon;
    private MonoBehaviour playerMovement;

    // =========================
    // 유니티 생명주기
    // =========================
    void Start()
    {
        // 플레이어 오브젝트와 컴포넌트 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            // 플레이어의 이동 컨트롤러를 MonoBehaviour로 참조 (구체 타입은 프로젝트에 따라 다름)
            playerMovement = playerObj.GetComponent<MonoBehaviour>();
        }

        // 대화 UI가 연결되지 않았다면 씬에서 자동 탐색
        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUIView>();
        }

        // 퀘스트 아이콘 초기 갱신
        UpdateQuestIcon();
    }

    /// <summary>
    /// 현재 퀘스트 상태에 따라 NPC 머리 위에 아이콘을 생성/제거합니다.
    /// </summary>
    public void UpdateQuestIcon()
    {
        // 기존 아이콘이 있으면 제거
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

        // 프리팹이 있으면 생성
        if (prefabToSpawn != null)
        {
            currentIcon = Instantiate(prefabToSpawn, transform.position + iconOffset, Quaternion.identity, transform);
        }
    }

    /// <summary>
    /// 매 프레임마다 플레이어와의 거리를 체크하고, 상호작용 입력을 처리합니다.
    /// </summary>
    void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 플레이어가 상호작용 범위 안에 들어왔을 때
        if (distanceToPlayer <= interactionRange)
        {
            // 상호작용 키 입력 감지
            if (Input.GetKeyDown(interactionKey))
            {
                // 이미 대화 UI가 열려있으면 다음 대사로 진행
                if (dialogueUI != null && dialogueUI.IsDialogueOpen())
                {
                    dialogueUI.HandleNextMessage(npcName);
                }
                else
                {
                    // 대화/상점/퀘스트 처리
                    InteractWithPlayer();
                }
            }
        }
        // 플레이어가 범위를 벗어나면 열린 UI 닫기
        else if (isUIOpen && distanceToPlayer > interactionRange)
        {
            CloseAllNPCUI();
        }
    }

    /// <summary>
    /// 플레이어 컨트롤(이동 등) 활성/비활성 설정
    /// </summary>
    /// <param name="state">활성(true) / 비활성(false)</param>
    void SetPlayerControl(bool state)
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = state;
        }
    }

    /// <summary>
    /// 플레이어와 상호작용 시작: 퀘스트 상태 확인, 대화 또는 상점 표시
    /// </summary>
    void InteractWithPlayer()
    {
        // 인벤토리에서 목표 아이템 개수 확인하여 퀘스트 상태 업데이트
        CheckQuestItemCount();

        isUIOpen = false;

        // 상점이 이미 열려있다면 닫기
        if (ShopPanel != null && ShopPanel.activeSelf)
        {
            CloseAllNPCUI();
            return;
        }

        isUIOpen = false;

        // 유효한 퀘스트가 있는 경우 퀘스트 상태에 따라 대화 및 후속 동작 결정
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
            // 퀘스트가 없는 NPC는 기본 대사 후 상점을 열거나 닫습니다.
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

    /// <summary>
    /// 시작 아이템을 지급하는 로직 (필요 시 구현)
    /// </summary>
    private void GiveStarterItems()
    {
        // 시작 아이템 지급 로직을 여기에 추가하세요.
    }

    /// <summary>
    /// 플레이어 인벤토리를 확인하여 퀘스트 목표 아이템 수량을 체크하고
    /// 조건을 만족하면 퀘스트 상태를 '완료 가능'으로 변경합니다.
    /// </summary>
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

    /// <summary>
    /// 지정된 패널(GameObject)을 화면에 표시하고, 플레이어 제어를 비활성화합니다.
    /// (예: 상점 패널)
    /// </summary>
    /// <param name="panelToShow">표시할 패널 오브젝트</param>
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

    /// <summary>
    /// 플레이어가 퀘스트를 수락했을 때 호출됩니다.
    /// 퀘스트 상태를 IN_PROGRESS로 바꾸고 HUD를 갱신합니다.
    /// </summary>
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

    /// <summary>
    /// 플레이어가 퀘스트를 거절했을 때 호출됩니다.
    /// </summary>
    public void RefuseQuest()
    {
        Debug.Log($"퀘스트 거절");
        CloseAllNPCUI();
    }

    // 비어있는 함수(확장 포인트)
    void CheckQuestCompletion() { }

    /// <summary>
    /// 퀘스트 완료 처리: 요구 아이템을 제거하고 보상 지급, 상태 갱신 등을 수행합니다.
    /// </summary>
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

    // 향후 상점 UI 표시 구현 포인트
    void ShowShopUI() { }

    /// <summary>
    /// 일반 대화창을 띄우는 헬퍼 함수입니다.
    /// </summary>
    /// <param name="name">화면에 표시할 NPC 이름</param>
    /// <param name="messages">표시할 대사 리스트</param>
    /// <param name="onAllHideComplete">대화 종료 후 실행할 콜백</param>
    void ShowGenericDialogue(string name, List<string> messages, Action onAllHideComplete)
    {
        if (dialogueUI != null)
        {
            SetPlayerControl(false);
            dialogueUI.ShowDialogueList(name, messages, onAllHideComplete, npcVoices);
            isUIOpen = true;
        }
    }

    /// <summary>
    /// 모든 NPC 관련 UI(대화창, 상점 등)를 닫고 플레이어 제어를 복원합니다.
    /// </summary>
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

    /// <summary>
    /// 상점에서 아이템을 구매할 때 호출되는 함수.
    /// 코인 부족시 구매 불가 로그를 출력합니다.
    /// </summary>
    /// <param name="itemToBuy">구매할 아이템 데이터</param>
    /// <param name="price">가격</param>
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

    /// <summary>
    /// NPC 관련 UI가 활성화되어 있는지 여부를 반환합니다.
    /// </summary>
    /// <returns>대화창 또는 상점이 열려 있으면 true</returns>
    public bool IsDialogueActive()
    {
        return isUIOpen || (ShopPanel != null && ShopPanel.activeSelf);
    }
}