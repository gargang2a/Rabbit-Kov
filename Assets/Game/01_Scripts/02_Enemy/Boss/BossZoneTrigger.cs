using UnityEngine;

/// <summary>
/// [Role] 보스 Zone 트리거 - 플레이어 진입 시 보스전 시작
/// 보스 오브젝트와 같은 부모에 배치하거나 별도 Zone 콜라이더에 부착
/// </summary>
[RequireComponent(typeof(Collider))]
public class BossZoneTrigger : MonoBehaviour
{
    [Header("보스 연결")]
    [Tooltip("활성화할 보스 컨트롤러")]
    [SerializeField] private BossController _boss;
    
    [Header("설정")]
    [Tooltip("플레이어 태그")]
    [SerializeField] private string _playerTag = "Player";
    
    [Tooltip("한 번만 활성화 (재진입 시 무시)")]
    [SerializeField] private bool _oneTimeActivation = true;
    
    private bool _hasTriggered = false;
    private Collider _zoneCollider;

    private void Awake()
    {
        _zoneCollider = GetComponent<Collider>();
        _zoneCollider.isTrigger = true;
        
        // 보스가 설정되지 않았으면 부모에서 찾기
        if (_boss == null)
        {
            _boss = GetComponentInParent<BossController>();
        }
        
        if (_boss == null)
        {
            Debug.LogWarning($"[BossZoneTrigger] {name}: BossController가 연결되지 않았습니다!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이미 트리거됐으면 무시
        if (_oneTimeActivation && _hasTriggered) return;
        
        // 플레이어인지 확인
        if (!other.CompareTag(_playerTag)) return;
        
        // 보스가 없으면 무시
        if (_boss == null) return;
        
        // 보스전 시작!
        _hasTriggered = true;
        _boss.StartBossFight(other.transform);
        
        Debug.Log($"[BossZoneTrigger] {_boss.name}: 보스전 시작!");
    }

    private void OnTriggerExit(Collider other)
    {
        // 일회성이 아닐 때만 퇴장 처리
        if (_oneTimeActivation) return;
        
        if (!other.CompareTag(_playerTag)) return;
        if (_boss == null) return;
        
        _boss.EndBossFight();
        _hasTriggered = false;
        
        Debug.Log($"[BossZoneTrigger] {_boss.name}: 플레이어 Zone 이탈, 보스전 종료");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;
        
        // 보스 Zone 시각화 (빨간색)
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
        }
    }
#endif
}
