using UnityEngine;

public class BagItemPickup : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("이 가방을 먹으면 늘어나는 최대 무게")]
    [SerializeField] private float _expandAmount = 20f;

    [Header("Audio")]
    [SerializeField] private AudioClip _pickupSound; // [New]
    [Range(0f, 0.5f)]
    [SerializeField] private float _pitchRandomness = 0.05f; // [New] 가방은 묵직하게 (변화폭 작게)

    private void Update()
    {
        transform.Rotate(Vector3.up * 50f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player == null) player = other.GetComponentInParent<Player>();

            if (player != null)
            {
                player.ExpandMaxWeight(_expandAmount);
                Debug.Log($"가방 획득! 인벤토리 무게 한도가 {_expandAmount}만큼 증가했습니다.");

                // ★ 사운드 재생 추가
                if (GlobalAudioManager.Instance != null && _pickupSound != null)
                {
                    GlobalAudioManager.Instance.PlaySFX(_pickupSound, _pitchRandomness);
                }

                Destroy(gameObject);
            }
        }
    }
}