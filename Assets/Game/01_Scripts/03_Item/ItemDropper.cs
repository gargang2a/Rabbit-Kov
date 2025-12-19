// ItemDropper.cs 파일 수정

using UnityEngine;

/// <summary>
/// 인벤토리 아이템을 월드에 물리적으로 드랍하는 시스템입니다.
/// 싱글톤 패턴을 사용하여 쉽게 접근할 수 있도록 합니다.
/// </summary>
public class ItemDropper : MonoBehaviour // ★ BaseSystem 대신 MonoBehaviour 직접 상속
{
    public static ItemDropper Instance { get; private set; } // ★ 싱글톤 인스턴스

    [Header("References")]
    [Tooltip("아이템이 던져질 위치. 보통 플레이어의 발 앞")]
    [SerializeField] private Transform _dropPoint;

    [Header("Drop Settings")]
    [Tooltip("아이템이 던져지는 힘의 세기")]
    [SerializeField] private float _dropForce = 5f;
    [Tooltip("아이템이 던져질 때의 랜덤 회전력")]
    [SerializeField] private float _dropTorque = 10f;

    private Transform _playerTransform;

    private void Awake()
    {
        // ★ 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 필요하다면 주석 해제 (씬 전환 시 유지)
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (_dropPoint == null)
        {
            _dropPoint = this.transform;
        }
        _playerTransform = this.transform;
    }

    // BaseSystem 관련 메서드는 이제 필요 없습니다.

    /// <summary>
    /// 월드에 아이템 프리팹을 드랍하고 물리 효과를 적용합니다.
    /// </summary>
    /// <param name="itemWorldPrefab">ItemData.worldPrefab에 설정된 아이템 프리팹</param>
    public void DropItem(GameObject itemWorldPrefab)
    {
        if (itemWorldPrefab == null)
        {
            Debug.LogError("드랍할 아이템 프리팹이 null입니다.");
            return;
        }

        // 1. 아이템 생성
        GameObject droppedItem = Instantiate(itemWorldPrefab, _dropPoint.position, Random.rotation);

        // 2. 물리 적용
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 플레이어가 바라보는 방향으로 던집니다.
            Vector3 dropDirection = (_playerTransform.forward * 0.5f + _playerTransform.up * 0.5f).normalized;
            rb.AddForce(dropDirection * _dropForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * _dropTorque, ForceMode.Impulse);
        }

        // 3. ItemHighlighter 활성화
        ItemHighlighter highlighter = droppedItem.GetComponent<ItemHighlighter>();
        if (highlighter != null)
        {
            highlighter.enabled = true;
            highlighter.ResetInitialPosition();
        }
    }
}