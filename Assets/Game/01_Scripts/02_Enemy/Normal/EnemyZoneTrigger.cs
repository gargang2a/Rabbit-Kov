using UnityEngine;

// Zone 트리거 - 플레이어 진입/퇴장 감지하여 부모 스포너에 알림
[RequireComponent(typeof(BoxCollider))]
public class EnemyZoneTrigger : MonoBehaviour
{
    private BoxCollider _collider;
    private EnemySpawner _spawner; // 부모 스포너

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
        _collider.isTrigger = true;
        _spawner = GetComponentInParent<EnemySpawner>();
        
        // Rigidbody 추가 (CharacterController 트리거 감지용)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        
        if (_spawner == null)
        {
            Debug.LogWarning($"[EnemyZoneTrigger] {gameObject.name}: EnemySpawner를 찾을 수 없음!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Player 태그만 처리 (다른 모든 것 무시)
        if (!other.CompareTag("Player")) return;
        
        // 자신의 부모 스포너에게만 알림
        _spawner?.OnPlayerEnterAnyZone(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        // 자신의 부모 스포너에게만 알림
        _spawner?.OnPlayerExitZone(_collider, other.transform);
    }
}
