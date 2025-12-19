using UnityEngine;

// [Role] 아이템 습득 로직 (수동 상호작용 + 자동 습득 하이브리드 지원)
public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Data")]
    public ItemData itemData; // 아이템 정보

    [Header("Settings")]
    [Tooltip("체크 시 F키 없이 닿기만 해도 획득됨 (경험치, 코인 등)")]
    [SerializeField] private bool _isAutoCollect = false;

    [Header("Audio")]
    [Tooltip("획득 시 재생할 사운드")]
    [SerializeField] private AudioClip _pickupSound;
    [Range(0f, 0.5f)]
    [SerializeField] private float _pitchRandomness = 0.1f;

    // [Mode 1] 인터페이스 구현: 상호작용(F키) 시 실행
    public void Interact(Player player)
    {
        TryCollect(player);
    }

    public string GetInteractPrompt()
    {
        return $"{itemData.itemName} 줍기";
    }

    // [Mode 2] 트리거 충돌 시 실행 (자동 습득 옵션이 켜져있을 때만)
    private void OnTriggerEnter(Collider other)
    {
        if (_isAutoCollect && other.CompareTag("Player"))
        {
            // 플레이어 컴포넌트를 가져와서 바로 획득 시도
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                TryCollect(player);
            }
        }
    }

    // [Common] 실제 인벤토리 추가 및 사운드 처리 로직 (공통 사용)
    private void TryCollect(Player player)
    {
        if (itemData == null) return;

        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null)
        {
            bool isAdded = inventory.AddItem(itemData);

            if (isAdded)
            {
                // 1. 사운드 재생 (GlobalAudioManager 사용)
                if (GlobalAudioManager.Instance != null && _pickupSound != null)
                {
                    GlobalAudioManager.Instance.PlaySFX(_pickupSound, _pitchRandomness);
                }

                Debug.Log($"{itemData.itemName} 획득!");

                // 2. 오브젝트 정리
                // [Opt] 서바이버 장르처럼 물량이 많으면 Destroy 대신 풀링(PoolManager.Return) 권장
                Destroy(gameObject);
            }
            else
            {
                // 인벤토리가 가득 찼을 때의 피드백 (예: UI 메시지)
                Debug.Log("인벤토리가 가득 찼습니다.");
            }
        }
    }
}