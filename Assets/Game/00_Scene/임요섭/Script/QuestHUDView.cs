using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class QuestHUDView : MonoBehaviour
{
    public static QuestHUDView Instance;

    [Header("UI 연결")]
    public GameObject hudPanel;

    [Header("UI 설정")]
    public GameObject questItemPrefab; // 1번에서 만든 프리팹 연결
    public Transform questListParent;  // Vertical Layout Group이 있는 부모 객체

    // 현재 표시 중인 퀘스트 항목들을 저장 (ID를 키값으로 사용)
    private Dictionary<int, GameObject> activeQuests = new Dictionary<int, GameObject>();
    private void Awake()
    {
        Instance = this;
        hudPanel.SetActive(false); // 처음엔 숨김
    }
    // NPC와 대화하여 퀘스트를 수락했을 때 호출할 함수
    public void UpdateQuestHUD(int questID, string title, string goalItem, int current, int required)
    {
        // 1. 패널이 꺼져 있다면 다시 활성화 (중요!)
        if (hudPanel != null && !hudPanel.activeSelf)
        {
            hudPanel.SetActive(true);
        }
        if (activeQuests.ContainsKey(questID)) 
        {
            var item = activeQuests[questID];
            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            // 텍스트 순서가 [Title, Goal]이라고 가정할 때
            if (texts.Length >= 2)
            {
                texts[1].text = $"{goalItem}: {current} / {required}";
            }
            return; 
        }
        // 새로운 퀘스트라면 프리팹 생성
        GameObject newQuest = Instantiate(questItemPrefab);
        // UI 좌표가 부모 패널의 기준점(Pivot)에 딱 붙도록 초기화
        RectTransform rt = newQuest.GetComponent<RectTransform>();
        newQuest.transform.SetParent(questListParent, false);
        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;
        var itemTexts = newQuest.GetComponentsInChildren<TextMeshProUGUI>();
        if (itemTexts.Length >= 2)
        {
            itemTexts[0].text = title;
            itemTexts[1].text = $"{goalItem}: {current} / {required}";
        }
        activeQuests.Add(questID, newQuest);
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(questListParent as RectTransform);
    }
    public void RemoveQuestHUD(int questID)
    {
        if (activeQuests.ContainsKey(questID))
        {
            Destroy(activeQuests[questID]); // UI 파괴 (Vertical Layout Group에 의해 아래 항목들이 자동 당겨짐)
            activeQuests.Remove(questID);
        }
    }
    // 퀘스트 완료 시 HUD를 끄는 함수
    public void HideHUD()
    {
        hudPanel.SetActive(false);
    }
}