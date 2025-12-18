using UnityEngine;

public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. 무엇이든 닿으면 일단 로그 출력 (범인 색출)
        Debug.Log($"킬존에 무언가 닿았습니다: {other.name} (태그: {other.tag})");

        // 2. 플레이어 태그 확인
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player == null) player = other.GetComponentInParent<Player>();

            if (player != null && !player.IsDead)
            {
                Debug.Log("플레이어 감지됨! 사망 처리 시작.");
                player.TakeDamage(99999);
            }
            else
            {
                Debug.Log("태그는 Player인데, Player 스크립트를 못 찾았습니다!");
            }
        }
    }
}