using UnityEngine;

public class NPCAppearanceController : MonoBehaviour
{
    [SerializeField] private GameObject npcObject; // 나타날 NPC 오브젝트
    private bool hasSpawned = false;
    void Update()
    {
        if (hasSpawned) return;

        // 매 프레임 모든 퀘스트가 완료되었는지 체크
        if (QuestManager.Instance != null && QuestManager.Instance.AreAllSnackQuestsDone())
        {
            SpawnNPC();
        }
    }

    void SpawnNPC()
    {
        if (npcObject != null)
        {
            npcObject.SetActive(true); // NPC 활성화
            hasSpawned = true;
            Debug.Log("조건 충족: 임용규 강사님이 맵에 나타났습니다!");

            // 필요 시 등장 사운드나 이펙트 재생 코드를 여기에 추가하세요.
        }
    }
}