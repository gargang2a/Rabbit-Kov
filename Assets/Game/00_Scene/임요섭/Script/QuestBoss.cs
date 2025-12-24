using UnityEngine;

public class QuestBoss : MonoBehaviour
{
    [Header("--- Boss Settings ---")]
    public GameObject bossPrefab;   // 소환할 보스 몬스터 프리팹
    public Transform spawnPoint;    // 보스가 나타날 위치 (빈 오브젝트 등)

    // 보스가 이미 소환되었는지 확인하는 플래그
    private bool _isSpawned = false;

    // NPC_Interaction에서 대화가 끝나면 호출할 함수
    public void SpawnBoss()
    {
        // 이미 소환된 적이 있다면 실행하지 않음 (1마리만 생성)
        if (_isSpawned) return;

        if (bossPrefab != null)
        {
            // 소환 위치가 지정되어 있으면 그곳에, 없으면 현재 위치에 소환
            Vector3 position = (spawnPoint != null) ? spawnPoint.position : transform.position;
            Quaternion rotation = (spawnPoint != null) ? spawnPoint.rotation : Quaternion.identity;

            Instantiate(bossPrefab, position, rotation);

            _isSpawned = true; // 소환 완료 체크
            Debug.Log("QuestBoss: 보스 몬스터가 소환되었습니다.");
        }
        else
        {
            Debug.LogError("QuestBoss: 보스 프리팹이 연결되지 않았습니다!");
        }
    }
}