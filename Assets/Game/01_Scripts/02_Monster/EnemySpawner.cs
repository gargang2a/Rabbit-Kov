using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 적 스폰 시스템 - Zone 기반 스폰 및 플레이어 감지 관리
public class EnemySpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject _enemyPrefab;      // 적 프리팹
    [SerializeField] private int maxSpawnCount = 5;        // 최대 스폰 수
    [SerializeField] private int _initialSpawnCount = 3;   // 초기 스폰 수
    [SerializeField] private float _spawnInterval = 10f;   // 스폰 간격 (초)
    [SerializeField] private int _spawnPerInterval = 1;    // 간격당 스폰 수

    [Header("스폰 구역")]
    [SerializeField] private BoxCollider[] _spawnZones;    // 스폰 가능 구역

    // Zone별 적 관리용 딕셔너리 (Key: Zone, Value: 해당 Zone의 적 리스트)
    private Dictionary<BoxCollider, List<EnemyController>> _zoneEnemies = new Dictionary<BoxCollider, List<EnemyController>>();
    private List<GameObject> _spawnedEnemies = new List<GameObject>(); // 전체 스폰된 적 목록
    private float _spawnTimer = 0f;    // 스폰 타이머
    private float _cleanupTimer = 0f;  // 정리 타이머
    private int _playerZoneCount = 0;  // 플레이어가 진입한 Zone 수
    private Transform _currentPlayer;  // 현재 추적 중인 플레이어

    private void Start()
    {
        // 스폰 Zone이 없으면 자식에서 자동 탐색
        if (_spawnZones == null || _spawnZones.Length == 0)
        {
            _spawnZones = GetComponentsInChildren<BoxCollider>();
        }

        // 모든 스폰 Zone 초기화
        foreach (var zone in _spawnZones)
        {
            _zoneEnemies[zone] = new List<EnemyController>(); // Zone별 적 리스트 생성
            zone.isTrigger = true; // 트리거로 설정 (플레이어 진입 감지용)
        }

        SpawnEnemies(_initialSpawnCount); // 초기 적 스폰
    }

    private void Update()
    {
        // 죽은 적 정리 (1초마다)
        _cleanupTimer += Time.deltaTime;
        if (_cleanupTimer >= 1f)
        {
            _cleanupTimer = 0f;
            CleanupDeadEnemies();
        }

        if (_spawnedEnemies.Count >= maxSpawnCount) return; // 최대치 도달 시 스폰 중단

        // 주기적 스폰
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            SpawnEnemies(_spawnPerInterval);
        }
    }

    // 지정된 수만큼 적 스폰
    private void SpawnEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_spawnedEnemies.Count >= maxSpawnCount) break; // 최대치 도달

            // 유효한 스폰 위치와 Zone 탐색 (out으로 Zone도 함께 반환)
            BoxCollider selectedZone;
            Vector3 spawnPos = FindValidSpawnPos(out selectedZone);
            
            if (spawnPos != Vector3.zero && selectedZone != null)
            {
                Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f); // Y축 랜덤 회전
                GameObject enemyObj = Instantiate(_enemyPrefab, spawnPos, randomRotation); // 적 생성
                _spawnedEnemies.Add(enemyObj); // 전체 목록에 추가

                // 적에게 Zone 할당
                EnemyController enemy = enemyObj.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.SetBoundZone(selectedZone); // 적의 이동 범위 설정
                    _zoneEnemies[selectedZone].Add(enemy); // Zone별 목록에 추가
                }
            }
        }
    }

    // 유효한 스폰 위치 탐색, out으로 선택된 Zone도 반환
    private Vector3 FindValidSpawnPos(out BoxCollider selectedZone)
    {
        selectedZone = null;
        
        if (_spawnZones == null || _spawnZones.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: 스폰 구역이 설정되지 않았습니다!");
            return Vector3.zero;
        }

        int maxAttempts = 30;   // 최대 시도 횟수
        float searchRadius = 50f; // NavMesh 탐색 반경 (m)

        // 최대 시도 횟수만큼 유효한 위치 탐색
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            BoxCollider zone = _spawnZones[Random.Range(0, _spawnZones.Length)]; // 랜덤 Zone 선택
            Bounds bounds = zone.bounds;
            
            // Zone 내 랜덤 포인트 생성
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x), // X: Zone 범위 내
                bounds.center.y,                          // Y: Zone 중심 높이
                Random.Range(bounds.min.z, bounds.max.z)  // Z: Zone 범위 내
            );

            // NavMesh 위 유효한 위치 탐색
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, searchRadius, NavMesh.AllAreas))
            {
                selectedZone = zone;  // 선택된 Zone 저장
                return hit.position;  // NavMesh 위 위치 반환
            }
        }

        Debug.LogWarning($"EnemySpawner: NavMesh 위치를 찾지 못했습니다. 구역 수: {_spawnZones.Length}");
        return Vector3.zero; // 실패 시 Vector3.zero 반환
    }

    // 플레이어 Zone 진입 시 호출 (EnemyZoneTrigger에서 호출)
    public void OnPlayerEnterAnyZone(Transform player)
    {
        _playerZoneCount++;         // Zone 카운트 증가
        _currentPlayer = player;    // 현재 플레이어 저장
        NotifyAllEnemiesEnter(player); // 모든 적에게 알림
    }

    // 플레이어 Zone 퇴장 시 호출 (EnemyZoneTrigger에서 호출)
    public void OnPlayerExitZone(BoxCollider exitedZone, Transform player)
    {
        _playerZoneCount--;
        if (_playerZoneCount < 0) _playerZoneCount = 0; // 음수 방지
        StartCoroutine(DelayedExitCheck()); // 지연 체크 (Zone 간 이동 대응)
    }

    // 지연된 퇴장 체크 (Zone 간 이동 시 즉시 퇴장 처리 방지)
    private System.Collections.IEnumerator DelayedExitCheck()
    {
        yield return new WaitForSeconds(0.1f); // 0.1초 대기
        
        // 모든 Zone에서 나갔으면 퇴장 처리
        if (_playerZoneCount == 0)
        {
            _currentPlayer = null;
            NotifyAllEnemiesExit();
        }
    }

    // 모든 적에게 플레이어 진입 알림
    private void NotifyAllEnemiesEnter(Transform player)
    {
        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy == null) continue; // null 체크
            var controller = enemy.GetComponent<EnemyController>();
            controller?.OnPlayerEnterZone(player); // 진입 이벤트 전달
        }
    }

    // 모든 적에게 플레이어 퇴장 알림
    private void NotifyAllEnemiesExit()
    {
        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy == null) continue; // null 체크
            var controller = enemy.GetComponent<EnemyController>();
            controller?.OnPlayerExitZone(); // 퇴장 이벤트 전달
        }
    }

    // 죽은 적 정리
    private void CleanupDeadEnemies()
    {
        _spawnedEnemies.RemoveAll(enemy => enemy == null); // 전체 목록에서 null 제거

        // Zone별 목록에서도 null 제거
        foreach (var zone in _spawnZones)
        {
            if (_zoneEnemies.ContainsKey(zone))
                _zoneEnemies[zone].RemoveAll(e => e == null);
        }
    }

#if UNITY_EDITOR
    // 스폰 구역 시각화 (에디터 전용)
    private void OnDrawGizmos()
    {
        BoxCollider[] zones = GetComponentsInChildren<BoxCollider>();
        
        foreach (BoxCollider zone in zones)
        {
            // 채워진 영역 (반투명 녹색)
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawCube(zone.bounds.center, zone.bounds.size);

            // 외곽선 (녹색)
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(zone.bounds.center, zone.bounds.size);
        }
    }
#endif
}
