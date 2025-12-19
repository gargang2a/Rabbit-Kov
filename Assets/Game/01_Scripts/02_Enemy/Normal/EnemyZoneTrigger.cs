using UnityEngine;

// [역할] Zone 트리거 - 플레이어 진입/퇴장 감지하여 스포너에 알림
[RequireComponent(typeof(BoxCollider))]
public class EnemyZoneTrigger : MonoBehaviour
{
    private BoxCollider _collider;    // Zone 콜라이더
    private EnemySpawner _spawner;    // 부모 스포너

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();   // 콜라이더 캐싱
        _collider.isTrigger = true;                // 트리거 설정
        _spawner = GetComponentInParent<EnemySpawner>(); // 부모 스포너 찾기
        
        // Rigidbody 추가 (CharacterController 트리거 감지용)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>(); // 없으면 추가
        }
        rb.isKinematic = true;  // 물리 영향 없음
        rb.useGravity = false;  // 중력 없음
        
        if (_spawner == null) // 스포너 없으면 경고
        {
            Debug.LogWarning($"[EnemyZoneTrigger] {gameObject.name}: EnemySpawner를 찾을 수 없음!");
        }
    }

    // 플레이어 진입 시
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return; // 플레이어만 처리
        
        _spawner?.OnPlayerEnterAnyZone(other.transform); // 스포너에 알림
    }

    // 플레이어 퇴장 시
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return; // 플레이어만 처리
        
        _spawner?.OnPlayerExitZone(_collider, other.transform); // 스포너에 알림
    }
}
