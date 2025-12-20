using UnityEngine;

public class BagItemPickup : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("이 가방을 먹으면 늘어나는 최대 무게")]
    [SerializeField] private float _expandAmount = 20f;

    [Header("Audio")]
    [SerializeField] private AudioClip _pickupSound;
    [Range(0f, 0.5f)]
    [SerializeField] private float _pitchRandomness = 0.05f;

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
                // 1. 데이터 변경 (최대 무게 증가)
                player.ExpandMaxWeight(_expandAmount);
                Debug.Log($"가방 획득! {_expandAmount}kg 증가.");

                // ★ [Fix] UI_WeightDisplay에게 "화면 다시 그려!"라고 명령
                if (UI_WeightDisplay.Instance != null)
                {
                    UI_WeightDisplay.Instance.ForceUpdate();
                }

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