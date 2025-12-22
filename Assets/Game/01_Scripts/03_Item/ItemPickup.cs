using UnityEngine;

<<<<<<< HEAD
=======
// [Role] 아이템 습득 로직 (인벤토리/탄약고 분기 처리 + 물리 충돌 무시)
>>>>>>> 32eed657e6815770054abd4842b9273b24719489
public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Data")]
    public ItemData itemData;

    [Header("Settings")]
    [Tooltip("이 아이템을 주웠을 때 획득하는 개수 (탄약일 경우 30발 등으로 설정)")]
    [SerializeField] private int _amount = 1; // ★ [New] 수량 설정

    [Tooltip("체크 시 F키 없이 닿기만 해도 획득됨")]
    [SerializeField] private bool _isAutoCollect = false;

    [Header("Audio")]
<<<<<<< HEAD
    [Tooltip("획득 시 재생할 사운드")]

=======
>>>>>>> 32eed657e6815770054abd4842b9273b24719489
    [SerializeField] private AudioClip _pickupSound;

    [Range(0f, 0.5f)]

    [SerializeField] private float _pitchRandomness = 0.1f;

<<<<<<< HEAD
=======
    // 시작 시 플레이어와의 물리적 충돌 무시
>>>>>>> 32eed657e6815770054abd4842b9273b24719489
    private void Start()
    {
        IgnoreCollisionWithPlayer();
    }
    private void IgnoreCollisionWithPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj == null) return;

        Collider playerCol = playerObj.GetComponent<Collider>();

        CharacterController playerCC = playerObj.GetComponent<CharacterController>();
<<<<<<< HEAD

=======
>>>>>>> 32eed657e6815770054abd4842b9273b24719489
        Collider[] myColliders = GetComponents<Collider>();
        foreach (Collider myCol in myColliders)
        {
            if (!myCol.isTrigger)
            {
                if (playerCol != null) Physics.IgnoreCollision(playerCol, myCol, true);
                if (playerCC != null) Physics.IgnoreCollision(playerCC, myCol, true);
            }
        }
    }
<<<<<<< HEAD
=======

    // F키 상호작용
>>>>>>> 32eed657e6815770054abd4842b9273b24719489
    public void Interact(Player player)
    {
        TryCollect(player);
    }
    public string GetInteractPrompt()
    {
        string amountStr = _amount > 1 ? $" x{_amount}" : "";
        return $"{itemData.itemName}{amountStr} 줍기";
    }
<<<<<<< HEAD
=======

    // 자동 습득
>>>>>>> 32eed657e6815770054abd4842b9273b24719489
    private void OnTriggerEnter(Collider other)
    {
        if (_isAutoCollect && other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                TryCollect(player);
            }
        }
    }
<<<<<<< HEAD
    private void TryCollect(Player player)
    {
        if (itemData == null) return;
        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null)
        {
            string itemNameForQuest = itemData.itemName;
            bool isAdded = inventory.AddItem(itemData);

            if (isAdded)
            {
                if (QuestHUDView.Instance != null)
                {
                    QuestHUDView.Instance.PickUpItem(itemNameForQuest);
                }
                if (GlobalAudioManager.Instance != null && _pickupSound != null)
                {
                    GlobalAudioManager.Instance.PlaySFX(_pickupSound, _pitchRandomness);
                }
                Debug.Log($"{itemData.itemName} 획득!");
                Destroy(gameObject);
            }
            else

            {
                Debug.Log("인벤토리가 가득 찼거나 너무 무겁습니다.");
=======

    // [Common] 획득 로직 (분기 처리)
    private void TryCollect(Player player)
    {
        if (itemData == null) return;

        bool isAdded = false;

        // ★ 1. 탄약(Ammo) 타입인지 확인
        if (itemData.itemType == ItemType.Ammo)
        {
            // 탄약 매니저(지갑)로 보냄
            PlayerAmmoManager ammoManager = player.GetComponent<PlayerAmmoManager>();
            if (ammoManager != null)
            {
                ammoManager.AddAmmo(itemData, _amount);
                isAdded = true;
            }
        }
        else
        {
            // ★ 2. 탄약이 아니면 인벤토리(가방)로 보냄
            Inventory inventory = player.GetComponent<Inventory>();
            if (inventory != null)
            {
                isAdded = inventory.AddItem(itemData);
>>>>>>> 32eed657e6815770054abd4842b9273b24719489
            }
        }

        // 결과 처리
        if (isAdded)
        {
            if (GlobalAudioManager.Instance != null && _pickupSound != null)
            {
                GlobalAudioManager.Instance.PlaySFX(_pickupSound, _pitchRandomness);
            }
            Debug.Log($"{itemData.itemName} ({_amount}개) 획득 성공!");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("획득 실패 (가방이 꽉 찼거나 무거움)");
        }
    }
}