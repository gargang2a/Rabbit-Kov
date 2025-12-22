using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Quest Objects")]
    public GameObject teacherQuestObject;

    [Header("Quest Completion Status")]
    public bool isFriedFoodDone = false;   // Æ¢±è Äù½ºÆ®
    public bool isSundaeDone = false;      // ¼ø´ë Äù½ºÆ®
    public bool isTteokbokkiDone = false;  // ¶±ººÀÌ Äù½ºÆ®

    [Header("Quest Settings")]
    public int goalAmount = 1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public bool IsQuestItem(string itemName)
    {
        return itemName.Contains("Æ¢±è") || itemName.Contains("¼ø´ë") || itemName.Contains("¶±ººÀÌ");
    }
    public (int id, string title, int required) GetQuestInfo(string itemName)
    {
        if (itemName.Contains("¼ø´ë")) return (101, "¼ø´ë ±¸ÇØ¿À±â", goalAmount);
        if (itemName.Contains("Æ¢±è")) return (102, "Æ¢±è ±¸ÇØ¿À±â", goalAmount);
        if (itemName.Contains("¶±ººÀÌ")) return (103, "¶±ººÀÌ ±¸ÇØ¿À±â", goalAmount);
        return (0, "", 0);
    }
    public void CheckAndShowTeacher()
    {
        if (isFriedFoodDone && isSundaeDone && isTteokbokkiDone)
        {
            if (teacherQuestObject != null)
            {
                teacherQuestObject.SetActive(true); // ¿ÀºêÁ§Æ® È°¼ºÈ­
                Debug.Log("¸ðµç Äù½ºÆ® ¿Ï·á! Teacher µîÀå.");
            }
        }
    }
    public bool AreAllSnackQuestsDone()
    {
        return isFriedFoodDone && isSundaeDone && isTteokbokkiDone;
    }
}