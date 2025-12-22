using UnityEngine;

// [역할] 보스 Zone 트리거 - 플레이어 진입 시 보스전 시작
[RequireComponent(typeof(Collider))]
public class BossZoneTrigger : MonoBehaviour
{
    [Header("보스 연결")]
    [Tooltip("활성화할 보스 컨트롤러 (비워두면 자동 탐색)")]
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
    }

    // 플레이어 진입
    private void OnTriggerEnter(Collider other)
    {
        if (_oneTimeActivation && _hasTriggered) return;
        if (!other.CompareTag(_playerTag)) return;
        
        // 보스가 없으면 동적으로 찾기
        if (_boss == null)
        {
            _boss = FindBossInZone();
        }
        
        if (_boss == null)
        {
            Debug.LogWarning($"[BossZoneTrigger] {name}: Zone 내 BossController를 찾을 수 없음!");
            return;
        }
        
        _hasTriggered = true;
        _boss.StartBossFight(other.transform);
        
        Debug.Log($"[BossZoneTrigger] {_boss.name}: 보스전 시작!");
    }

    // 플레이어 퇴장
    private void OnTriggerExit(Collider other)
    {
        if (_oneTimeActivation) return;
        if (!other.CompareTag(_playerTag)) return;
        if (_boss == null) return;
        
        _boss.EndBossFight();
        _hasTriggered = false;
        
        Debug.Log($"[BossZoneTrigger] {_boss.name}: 보스전 종료");
    }
    
    // Zone 내에서 BossController 찾기
    private BossController FindBossInZone()
    {
        // 방법 1: 부모에서 찾기
        BossController boss = GetComponentInParent<BossController>();
        if (boss != null) return boss;
        
        // 방법 2: Zone 범위 내에서 찾기
        Collider[] colliders = Physics.OverlapSphere(transform.position, GetZoneRadius());
        foreach (var col in colliders)
        {
            boss = col.GetComponent<BossController>();
            if (boss == null) boss = col.GetComponentInParent<BossController>();
            if (boss != null) return boss;
        }
        
        // 방법 3: 씬 전체에서 찾기 (fallback)
        return FindObjectOfType<BossController>();
    }
    
    // Zone 반경 계산
    private float GetZoneRadius()
    {
        if (_zoneCollider is SphereCollider sphere)
            return sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        if (_zoneCollider is BoxCollider box)
            return Mathf.Max(box.size.x, box.size.y, box.size.z) * 0.5f;
        return 20f; // 기본값
    }
    
    // 외부에서 보스 설정 (EnemySpawner에서 호출 가능)
    public void SetBoss(BossController boss)
    {
        _boss = boss;
        Debug.Log($"[BossZoneTrigger] 보스 연결됨: {boss?.name}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;
        
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
