using UnityEngine;

// [역할] 보스 Zone 트리거 - 플레이어 진입 시 보스전 시작
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
    
    private bool _hasTriggered = false;   // 트리거 여부
    private Collider _zoneCollider;       // Zone 콜라이더

    private void Awake()
    {
        _zoneCollider = GetComponent<Collider>();
        _zoneCollider.isTrigger = true;
        
        if (_boss == null) // 보스 없으면 부모에서 찾기
        {
            _boss = GetComponentInParent<BossController>();
        }
        
        if (_boss == null)
        {
            Debug.LogWarning($"[BossZoneTrigger] {name}: BossController 없음!");
        }
    }

    // 플레이어 진입
    private void OnTriggerEnter(Collider other)
    {
        if (_oneTimeActivation && _hasTriggered) return; // 이미 트리거
        if (!other.CompareTag(_playerTag)) return;       // 플레이어 아님
        if (_boss == null) return;                       // 보스 없음
        
        _hasTriggered = true;
        _boss.StartBossFight(other.transform);
        
        Debug.Log($"[BossZoneTrigger] {_boss.name}: 보스전 시작!");
    }

    // 플레이어 퇴장
    private void OnTriggerExit(Collider other)
    {
        if (_oneTimeActivation) return; // 일회성이면 무시
        if (!other.CompareTag(_playerTag)) return;
        if (_boss == null) return;
        
        _boss.EndBossFight();
        _hasTriggered = false;
        
        Debug.Log($"[BossZoneTrigger] {_boss.name}: 보스전 종료");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;
        
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // 빨간색
        
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
