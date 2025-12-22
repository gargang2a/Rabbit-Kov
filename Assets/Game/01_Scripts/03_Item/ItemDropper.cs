using UnityEngine;

public class ItemDropper : MonoBehaviour
{
    public static ItemDropper Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform _dropPoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (_dropPoint == null) _dropPoint = this.transform;
    }
    
    public void DropItem(GameObject itemWorldPrefab)
    {
        if (itemWorldPrefab == null)
        {
            Debug.LogError("드랍할 아이템 프리팹이 null입니다.");
            return;
        }

        // 1. 위치 계산 (플레이어 위치 + Y 1.0f)
        Vector3 spawnPosition = _dropPoint.position;
        spawnPosition.y += 1.0f;

        // 2. ★ [핵심 수정] 프리팹의 기본 회전값(Rotation)을 그대로 사용
        // 기존: Quaternion.identity (0,0,0 강제)
        // 수정: itemWorldPrefab.transform.rotation (프리팹에 설정된 -90도 등 유지)
        GameObject droppedItem = Instantiate(itemWorldPrefab, spawnPosition, itemWorldPrefab.transform.rotation);

        // 3. 물리 간섭 제거 (Kinematic)
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = true;
        }

        // 4. ItemHighlighter 활성화
        ItemHighlighter highlighter = droppedItem.GetComponent<ItemHighlighter>();
        if (highlighter != null)
        {
            highlighter.enabled = true;
            highlighter.ResetInitialPosition();
        }
    }
}