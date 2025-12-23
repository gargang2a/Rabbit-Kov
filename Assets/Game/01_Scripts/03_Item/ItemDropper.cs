using UnityEngine;

public class ItemDropper : MonoBehaviour
{
    public static ItemDropper Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform _dropPoint;

    [Header("Settings")]
    // ?? [Maintenance] 하드코딩 대신 인스펙터에서 조절 가능한 변수로 분리
    [Tooltip("아이템이 드랍될 고정 Y 높이입니다.")]
    [SerializeField] private float _fixedDropHeight = 2.0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 방어 코드: dropPoint가 할당되지 않았다면 자신의 Transform 사용
        if (_dropPoint == null) _dropPoint = this.transform;
    }

    public void DropItem(GameObject itemWorldPrefab)
    {
        if (itemWorldPrefab == null)
        {
            Debug.LogError("[ItemDropper] 드랍할 아이템 프리팹이 null입니다.");
            return;
        }

        // 1. 위치 계산
        // _dropPoint의 X, Z 좌표는 유지하되, Y 좌표만 설정값으로 덮어씌웁니다.
        Vector3 spawnPosition = _dropPoint.position;
        spawnPosition.y = _fixedDropHeight; // ★ [핵심 수정] += 가 아닌 = 로 절대값 할당

        // 2. 프리팹의 기본 회전값(Rotation) 유지
        GameObject droppedItem = Instantiate(itemWorldPrefab, spawnPosition, itemWorldPrefab.transform.rotation);

        // 3. 물리 간섭 제거 (Kinematic 설정)
        // ?? [Style] 변수명을 명확하게 변경 (rb -> itemRigidbody)
        Rigidbody itemRigidbody = droppedItem.GetComponent<Rigidbody>();
        if (itemRigidbody != null)
        {
            itemRigidbody.velocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
            itemRigidbody.isKinematic = true; // 중력 영향 받지 않고 해당 위치(Y=2)에 고정됨
            itemRigidbody.detectCollisions = true;
        }

        // 4. ItemHighlighter 활성화
        // ?? [Style] 변수명을 명확하게 변경 (highlighter -> itemHighlighter)
        ItemHighlighter itemHighlighter = droppedItem.GetComponent<ItemHighlighter>();
        if (itemHighlighter != null)
        {
            itemHighlighter.enabled = true;
            itemHighlighter.ResetInitialPosition();
        }
    }
}