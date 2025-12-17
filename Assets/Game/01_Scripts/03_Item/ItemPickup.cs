using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Data")]
    public ItemData itemData; // 이 오브젝트가 무슨 아이템인지

    // 인터페이스 구현: 상호작용(F키) 시 실행
    public void Interact(Player player)
    {
        if (itemData == null) return;

        // 플레이어의 인벤토리를 찾아 아이템 추가 시도
        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null)
        {
            bool isAdded = inventory.AddItem(itemData);
            if (isAdded)
            {
                Debug.Log($"{itemData.itemName} 획득!");

                // 효과음 재생 등 추가 가능

                // 씬에서 오브젝트 삭제
                Destroy(gameObject);
            }
        }
    }

    public string GetInteractPrompt()
    {
        return $"{itemData.itemName} 줍기";
    }
}