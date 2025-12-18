using UnityEngine;

public class BagItemPickup : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("이 가방을 먹으면 늘어나는 최대 무게")]
    [SerializeField] private float _expandAmount = 20f;

    // 가방이 제자리에서 빙글빙글 도는 효과 (선택 사항)
    private void Update()
    {
        transform.Rotate(Vector3.up * 50f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어와 충돌했는지 확인
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();

            // 만약 Player 컴포넌트가 부모에 있다면 찾기
            if (player == null) player = other.GetComponentInParent<Player>();

            if (player != null)
            {
                // 플레이어의 최대 무게를 늘림
                player.ExpandMaxWeight(_expandAmount);

                Debug.Log($"가방 획득! 인벤토리 무게 한도가 {_expandAmount}만큼 증가했습니다.");

                // 가방 오브젝트 삭제
                Destroy(gameObject);
            }
        }
    }
}