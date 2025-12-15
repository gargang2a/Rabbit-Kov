using UnityEngine;

// Zone 트리거 - 플레이어 진입/퇴장 감지하여 스포너에 알림
[RequireComponent(typeof(BoxCollider))]
public class EnemyZoneTrigger : MonoBehaviour
{
    private BoxCollider _collider;    // Zone 영역
    private EnemySpawner _spawner;    // 부모 스포너 참조

    // 컴포넌트 캐싱
    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
        _collider.isTrigger = true; // 트리거로 설정
        _spawner = GetComponentInParent<EnemySpawner>(); // 부모에서 스포너 탐색
    }

    // 플레이어 진입 시 스포너에 알림
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return; // Player 태그만 처리
        _spawner?.OnPlayerEnterAnyZone(other.transform);
    }

    // 플레이어 퇴장 시 스포너에 알림
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return; // Player 태그만 처리
        _spawner?.OnPlayerExitZone(_collider, other.transform);
    }
}
