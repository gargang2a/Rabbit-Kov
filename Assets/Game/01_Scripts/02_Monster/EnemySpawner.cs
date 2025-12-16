using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 적 스폰 시스템 - Zone 기반 스폰 및 플레이어 감지 관리
public class EnemySpawner : MonoBehaviour
{
    [Header("Normal 몬스터 설정")]
    [SerializeField] private GameObject _normalEnemyPrefab;     // Normal 몬스터 프리팹
    [SerializeField] private int _normalMaxCount = 5;           // Normal 최대 스폰 수
    [SerializeField] private int _normalInitialCount = 3;       // Normal 초기 스폰 수
    [SerializeField] private float _normalSpawnInterval = 10f;  // Normal 스폰 간격 (초)
    [SerializeField] private int _normalSpawnPerInterval = 1;   // 간격당 Normal 스폰 수

    [Header("Epic 몬스터 설정")]
    [SerializeField] private GameObject _epicEnemyPrefab;       // Epic 몬스터 프리팹
    [SerializeField] private int _epicMaxCount = 2;             // Epic 최대 스폰 수
    [SerializeField] private int _epicInitialCount = 1;         // Epic 초기 스폰 수
    [SerializeField] private float _epicSpawnInterval = 30f;    // Epic 스폰 간격 (초)
    [SerializeField] private int _epicSpawnPerInterval = 1;     // 간격당 Epic 스폰 수

    [Header("스폰 구역")]
    [SerializeField] private BoxCollider[] _spawnZones;         // 스폰 가능 구역
    
    [Header("이동 설정")]
    [Tooltip("true: 적이 모든 Zone을 자유롭게 이동 / false: 스폰된 Zone 내에서만 이동")]
    [SerializeField] private bool _allowFreeMovement = true;    // 자유 이동 허용

    // Zone별 적 관리용 딕셔너리 (Key: Zone, Value: 해당 Zone의 적 리스트)
    private Dictionary<BoxCollider, List<EnemyController>> _zoneEnemies = new Dictionary<BoxCollider, List<EnemyController>>();
    private List<GameObject> _spawnedNormalEnemies = new List<GameObject>(); // Normal 몬스터 목록
    private List<GameObject> _spawnedEpicEnemies = new List<GameObject>();   // Epic 몬스터 목록
    private float _normalSpawnTimer = 0f;  // Normal 스폰 타이머
    private float _epicSpawnTimer = 0f;    // Epic 스폰 타이머
    private float _cleanupTimer = 0f;      // 정리 타이머
    private int _playerZoneCount = 0;      // 플레이어가 진입한 Zone 수
    private Transform _currentPlayer;      // 현재 추적 중인 플레이어
    private bool _hasPlayerEnteredZone = false; // 플레이어 Zone 진입 여부 (스폰 시작 조건)

    private void Start()
    {
        // 스폰 Zone이 없으면 자식에서 자동 탐색
        if (_spawnZones == null || _spawnZones.Length == 0)
        {
            _spawnZones = GetComponentsInChildren<BoxCollider>();
        }
        
        // Zone이 없으면 경고
        if (_spawnZones.Length == 0)
        {
            return;
        }

        // 모든 스폰 Zone 초기화
        foreach (var zone in _spawnZones)
        {
            if (zone == null) continue;
            
            _zoneEnemies[zone] = new List<EnemyController>(); // Zone별 적 리스트 생성
            zone.isTrigger = true; // 트리거로 설정 (플레이어 진입 감지용)
            
            // [Auto-Fix] 트리거 스크립트가 없으면 자동으로 추가
            if (zone.GetComponent<EnemyZoneTrigger>() == null)
            {
                zone.gameObject.AddComponent<EnemyZoneTrigger>();
            }
        }
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

        // 플레이어가 Zone에 있을 때만 스폰 진행
        if (!_hasPlayerEnteredZone) return;

        // Normal 몬스터 주기적 스폰
        _normalSpawnTimer += Time.deltaTime;
        if (_normalSpawnTimer >= _normalSpawnInterval)
        {
            _normalSpawnTimer = 0f;
            if (_spawnedNormalEnemies.Count < _normalMaxCount)
                SpawnEnemiesByType(_normalEnemyPrefab, _normalSpawnPerInterval, _spawnedNormalEnemies, _normalMaxCount);
        }

        // Epic 몬스터 주기적 스폰
        _epicSpawnTimer += Time.deltaTime;
        if (_epicSpawnTimer >= _epicSpawnInterval)
        {
            _epicSpawnTimer = 0f;
            if (_spawnedEpicEnemies.Count < _epicMaxCount)
                SpawnEnemiesByType(_epicEnemyPrefab, _epicSpawnPerInterval, _spawnedEpicEnemies, _epicMaxCount);
        }
    }

    // 유형별 적 스폰 (프리팹, 스폰 수, 목록, 최대치)
    private void SpawnEnemiesByType(GameObject prefab, int count, List<GameObject> enemyList, int maxCount)
    {
        if (prefab == null) return; // 프리팹 없으면 패스

        for (int i = 0; i < count; i++)
        {
            if (enemyList.Count >= maxCount) break; // 최대치 도달

            // 유효한 스폰 위치와 Zone 탐색 (out으로 Zone도 함께 반환)
            BoxCollider selectedZone;
            Vector3 spawnPos = FindValidSpawnPos(out selectedZone);
            
            if (spawnPos != Vector3.zero && selectedZone != null)
            {
                Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f); // Y축 랜덤 회전
                GameObject enemyObj = Instantiate(prefab, spawnPos, randomRotation); // 적 생성
                enemyList.Add(enemyObj); // 유형별 목록에 추가

                // 적에게 Zone 할당 (자유 이동 설정에 따라)
                EnemyController enemy = enemyObj.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    // 자유 이동 허용 시 Zone 제한 없음, 아니면 모든 Zone 내부로 제한
                    if (_allowFreeMovement)
                    {
                        enemy.SetBoundZones(null);
                    }
                    else
                    {
                        // 모든 Cube를 BoundZones로 설정 (Zone 내 자유 이동, Zone 밖은 금지)
                        enemy.SetBoundZones(_spawnZones);
                    }
                    _zoneEnemies[selectedZone].Add(enemy); // Zone별 목록에 추가
                    
                    // Normal 몬스터만 즉시 타겟 전달 (Zone 진입 시 돌진)
                    // Epic 몬스터는 EnemySenses가 감지할 때까지 PatrolState 유지
                    if (_currentPlayer != null && _hasPlayerEnteredZone && !enemy.IsEpic)
                    {
                        enemy.OnPlayerEnterZone(_currentPlayer);
                    }
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

        int maxAttempts = 30;       // 최대 시도 횟수
        float searchRadius = 100f;   // NavMesh 탐색 반경 (m) - Zone이 공중에 있을 수 있으므로 넓게
        
        int navMeshFailCount = 0;   // NavMesh 탐색 실패 횟수
        int boundsFailCount = 0;    // Zone 범위 검증 실패 횟수

        // 최대 시도 횟수만큼 유효한 위치 탐색
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            BoxCollider zone = _spawnZones[Random.Range(0, _spawnZones.Length)]; // 랜덤 Zone 선택
            Bounds bounds = zone.bounds;
            
            // Zone 내 랜덤 포인트 생성
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            // NavMesh 위 유효한 위치 탐색
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, searchRadius, NavMesh.AllAreas))
            {
                // XZ 평면에서 Zone 내부인지 검증 (Y 좌표 무시)
                Vector3 hitPosXZ = new Vector3(hit.position.x, bounds.center.y, hit.position.z);
                if (bounds.Contains(hitPosXZ))
                {
                    selectedZone = zone;
                    return hit.position;
                }
                else
                {
                    boundsFailCount++;
                }
            }
            else
            {
                navMeshFailCount++;
            }
        }

        // 디버그 로그: 실패 원인 상세 출력
        Debug.LogWarning($"EnemySpawner 스폰 실패 분석:\n" +
            $"- 총 시도: {maxAttempts}회\n" +
            $"- NavMesh 탐색 실패: {navMeshFailCount}회 (NavMesh가 없거나 탐색 반경 밖)\n" +
            $"- Zone 범위 검증 실패: {boundsFailCount}회 (NavMesh 위치가 Zone 밖)\n" +
            $"- Zone 개수: {_spawnZones.Length}\n" +
            $"- 첫 Zone 크기: {_spawnZones[0].bounds.size}\n" +
            $"- 첫 Zone 위치: {_spawnZones[0].bounds.center}");
        
        return Vector3.zero;
    }

    // 플레이어 Zone 진입 시 호출 (EnemyZoneTrigger에서 호출)
    public void OnPlayerEnterAnyZone(Transform player)
    {
        _playerZoneCount++;         // Zone 카운트 증가
        _currentPlayer = player;    // 현재 플레이어 저장
        
        // 처음 Zone 진입 시 초기 스폰 수행
        if (!_hasPlayerEnteredZone)
        {
            _hasPlayerEnteredZone = true;
            SpawnEnemiesByType(_normalEnemyPrefab, _normalInitialCount, _spawnedNormalEnemies, _normalMaxCount);
            SpawnEnemiesByType(_epicEnemyPrefab, _epicInitialCount, _spawnedEpicEnemies, _epicMaxCount);
        }
        
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
        NotifyEnemyList(_spawnedNormalEnemies, player, true);
        NotifyEnemyList(_spawnedEpicEnemies, player, true);
    }

    // 모든 적에게 플레이어 퇴장 알림
    private void NotifyAllEnemiesExit()
    {
        NotifyEnemyList(_spawnedNormalEnemies, null, false);
        NotifyEnemyList(_spawnedEpicEnemies, null, false);
    }

    // 적 리스트에 이벤트 알림
    private void NotifyEnemyList(List<GameObject> enemyList, Transform player, bool isEnter)
    {
        foreach (var enemy in enemyList)
        {
            if (enemy == null) continue;
            var controller = enemy.GetComponent<EnemyController>();
            if (controller == null) continue;

            if (isEnter)
            {
                // Epic 몬스터는 EnemySenses가 감지할 때까지 PatrolState 유지
                // Normal 몬스터만 즉시 타겟 전달
                if (!controller.IsEpic)
                {
                    controller.OnPlayerEnterZone(player);
                }
            }
            else
            {
                controller.OnPlayerExitZone();
            }
        }
    }

    // 죽은 적 정리
    private void CleanupDeadEnemies()
    {
        _spawnedNormalEnemies.RemoveAll(enemy => enemy == null); // Normal 목록에서 null 제거
        _spawnedEpicEnemies.RemoveAll(enemy => enemy == null);   // Epic 목록에서 null 제거

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
        // 유효한 Zone 수집
        List<BoxCollider> drawList = new List<BoxCollider>();
        
        // 1. 인스펙터 리스트 확인
        if (_spawnZones != null && _spawnZones.Length > 0)
        {
            foreach (var z in _spawnZones)
            {
                if (z != null) drawList.Add(z);
            }
        }

        // 2. 인스펙터에 유효한 게 하나도 없으면 자식에서 탐색
        if (drawList.Count == 0)
        {
            drawList.AddRange(GetComponentsInChildren<BoxCollider>());
        }

        // 3. 그리기
        foreach (BoxCollider zone in drawList)
        {
            if (zone == null) continue;

            // 채워진 영역 (반투명 녹색)
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawCube(zone.bounds.center, zone.bounds.size);

            // 외곽선 (녹색) (스포너 시각화임이 명확하도록 약간 진하게)
            Gizmos.color = new Color(0f, 1f, 0f, 1f); 
            Gizmos.DrawWireCube(zone.bounds.center, zone.bounds.size);
        }
    }
#endif
}
