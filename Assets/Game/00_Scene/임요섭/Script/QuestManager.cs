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