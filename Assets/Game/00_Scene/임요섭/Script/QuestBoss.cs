using UnityEngine;

public class QuestBoss : MonoBehaviour
{
    [Header("--- Boss Zone Settings ---")]
    [Tooltip("씬에 미리 배치된 BossZone (비활성 상태)")]
    public GameObject bossZone;   // 미리 배치된 BossZone (비활성 상태)

    // 보스 존이 이미 활성화되었는지 확인하는 플래그
    private bool _isActivated = false;

    // NPC_Interaction에서 대화가 끝나면 호출될 함수
    public void SpawnBoss()
    {
        // 이미 활성화된 경우 무시 (1회성 활성화)
        if (_isActivated) return;

        if (bossZone != null)
        {
            // BossZone 활성화
            bossZone.SetActive(true);

            _isActivated = true; // 활성화 완료 체크
            Debug.Log("[QuestBoss] 보스 존이 활성화되었습니다!");
        }
        else
        {
            Debug.LogError("[QuestBoss] BossZone이 할당되지 않았습니다! Inspector에서 연결하세요.");
        }
    }
}