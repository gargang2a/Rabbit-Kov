using UnityEngine;

public class AccessoryPickup : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("감소시킬 탄퍼짐 각도")]
    [SerializeField] private float _spreadReductionAmount = 2.0f;

    [Header("Audio")]
    [SerializeField] private AudioClip _pickupSound;
    [Range(0f, 0.5f)]
    [SerializeField] private float _pitchRandomness = 0.1f;

    private void Update()
    {
        transform.Rotate(Vector3.up * 50f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. 플레이어 컴포넌트 찾기
            Player player = other.GetComponent<Player>();
            if (player == null) player = other.GetComponentInParent<Player>();

            if (player != null)
            {
                // ★ [Fix] 총이 아니라 플레이어의 스탯을 영구적으로 올림
                player.AcquireVerticalGrip(_spreadReductionAmount);

                Debug.Log($"수직 손잡이 획득! 반동 {_spreadReductionAmount} 감소.");

                // 2. 사운드 재생
                if (GlobalAudioManager.Instance != null && _pickupSound != null)
                {
                    GlobalAudioManager.Instance.PlaySFX(_pickupSound, _pitchRandomness);
                }

                Destroy(gameObject);
            }
        }
    }
}