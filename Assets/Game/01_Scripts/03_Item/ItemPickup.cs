using UnityEngine;

// [Role] 아이템 습득 로직 (상호작용/자동습득 + 물리 충돌 무시)
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

    // ★ 시작 시 플레이어와의 물리적 충돌을 무시하도록 설정 (밟고 올라서는 현상 방지)
    private void Start()
    {
        IgnoreCollisionWithPlayer();
    }

    private void IgnoreCollisionWithPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        // 플레이어의 콜라이더 (CharacterController 포함)
        Collider playerCol = playerObj.GetComponent<Collider>();
        CharacterController playerCC = playerObj.GetComponent<CharacterController>();

        // 내 몸에 붙은 모든 콜라이더 (물리용 Box, 감지용 Sphere 등)
        Collider[] myColliders = GetComponents<Collider>();

        foreach (Collider myCol in myColliders)
        {
            // 'Is Trigger'가 꺼져있는(단단한) 콜라이더만 무시 설정
            // (Trigger가 켜진 줍기용 콜라이더는 끄면 안 됨!)
            if (!myCol.isTrigger)
            {
                if (playerCol != null) Physics.IgnoreCollision(playerCol, myCol, true);
                if (playerCC != null) Physics.IgnoreCollision(playerCC, myCol, true);
            }
        }
    }

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
            // 무게 체크 후 추가 시도 (실패 시 false 반환)
            bool isAdded = inventory.AddItem(itemData);

            if (isAdded)
            {
                // 1. 사운드 재생 (일반적인 랜덤 피치 방식)
                if (GlobalAudioManager.Instance != null && _pickupSound != null)
                {
                    GlobalAudioManager.Instance.PlaySFX(_pickupSound, _pitchRandomness);
                }

                Debug.Log($"{itemData.itemName} 획득!");

                // 2. 오브젝트 정리
                Destroy(gameObject);
            }
            else
            {
                // 인벤토리가 가득 찼거나 무거울 때
                Debug.Log("인벤토리가 가득 찼거나 너무 무겁습니다.");
            }
        }
    }
}