using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Data")]
    public ItemData itemData;

    [Header("Settings")]
    [Tooltip("Ã¼Å© ½Ã FÅ° ¾øÀÌ ´ê±â¸¸ ÇØµµ È¹µæµÊ (°æÇèÄ¡, ÄÚÀÎ µî)")]
    [SerializeField] private bool _isAutoCollect = false;

    [Header("Audio")]
    [Tooltip("È¹µæ ½Ã Àç»ýÇÒ »ç¿îµå")]

    [SerializeField] private AudioClip _pickupSound;

    [Range(0f, 0.5f)]

    [SerializeField] private float _pitchRandomness = 0.1f;

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
    public void Interact(Player player)
    {
        TryCollect(player);
    }
    public string GetInteractPrompt()
    {
        return $"{itemData.itemName} ÁÝ±â";
    }
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
                Debug.Log($"{itemData.itemName} È¹µæ!");
                Destroy(gameObject);
            }
            else

            {
                Debug.Log("ÀÎº¥Åä¸®°¡ °¡µæ Ã¡°Å³ª ³Ê¹« ¹«°Ì½À´Ï´Ù.");
            }
        }
    }
}