using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class QuestHUDView : MonoBehaviour
{
    public static QuestHUDView Instance;

    [Header("UI ¿¬°á")]
    public GameObject hudPanel;

    [Header("UI ¼³Á¤")]
    public GameObject questItemPrefab;
    public Transform questListParent;

    private Dictionary<int, GameObject> activeQuests = new Dictionary<int, GameObject>();
    private void Awake()
    {
        Instance = this;
        hudPanel.SetActive(false);
    }
    public void UpdateQuestHUD(int questID, string title, string goalItem, int current, int required)
    {
        if (hudPanel != null && !hudPanel.activeSelf)
        {
            hudPanel.SetActive(true);
        }
        if (activeQuests.ContainsKey(questID))
        {
            GameObject item = activeQuests[questID];
            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 2)
            {
                texts[1].text = $"      {current} / {required}";
                texts[1].color = (current >= required) ? Color.green : Color.white;
            }
            return;
        }
        GameObject newQuest = Instantiate(questItemPrefab);

        newQuest.transform.SetParent(questListParent, false);

        RectTransform rt = newQuest.GetComponent<RectTransform>();
        var itemTexts = newQuest.GetComponentsInChildren<TextMeshProUGUI>();
        if (itemTexts.Length >= 2)
        {
            itemTexts[0].text = title;
            itemTexts[0].color = Color.yellow;
            itemTexts[1].text = $"      {current} / {required}";
            itemTexts[1].color = Color.white;
            if (current >= required)
            {
                itemTexts[1].color = Color.green;
            }
        }
        activeQuests.Add(questID, newQuest);
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(questListParent as RectTransform);
    }
    public void RemoveQuestHUD(int questID)
    {
        if (activeQuests.ContainsKey(questID))
        {
            Destroy(activeQuests[questID]);
            activeQuests.Remove(questID);
        }
    }
    public void HideHUD()
    {
        hudPanel.SetActive(false);
    }
    public void PickUpItem(string pickedItemName)
    {
        string cleanName = pickedItemName.Replace("(Clone)", "").Trim();

        if (QuestManager.Instance != null && QuestManager.Instance.IsQuestItem(cleanName))
        {
            var info = QuestManager.Instance.GetQuestInfo(cleanName);
            Inventory inventory = GameObject.FindGameObjectWithTag("Player").GetComponent<Inventory>();
            int currentAmount = 0;
            if (inventory != null)
            {
                foreach (var item in inventory.Items)
                {
                    if (item != null && item.itemName == cleanName) currentAmount++;
                }
            }
            UpdateQuestHUD(info.id, info.title, cleanName, currentAmount, info.required);

            if (currentAmount >= info.required)
            {
                if (cleanName.Contains("Æ¢±è")) QuestManager.Instance.isFriedFoodDone = true;
                if (cleanName.Contains("¼ø´ë")) QuestManager.Instance.isSundaeDone = true;
                if (cleanName.Contains("¶±ººÀÌ")) QuestManager.Instance.isTteokbokkiDone = true;

                QuestManager.Instance.CheckAndShowTeacher();
            }
        }
    }
}