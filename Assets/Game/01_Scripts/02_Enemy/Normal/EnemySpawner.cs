using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 적 스폰 시스템 - Zone 기반 스폰 및 플레이어 감지 관리
public class EnemySpawner : MonoBehaviour
{
    [Header("Normal 몬스터 설정")]
    [SerializeField] private GameObject[] _normalEnemyPrefabs;   // Normal 몬스터 프리팹 (랜덤 선택)
    [SerializeField] private int _normalMaxCount = 5;            // Normal 최대 스폰 수
    [SerializeField] private int _normalInitialCount = 3;        // Normal 초기 스폰 수
    [SerializeField] private float _normalSpawnInterval = 10f;   // Normal 스폰 간격 (초)
    [SerializeField] private int _normalSpawnPerInterval = 1;    // 간격당 Normal 스폰 수

    [Header("Epic 몬스터 설정")]
    [SerializeField] private GameObject[] _epicEnemyPrefabs;     // Epic 몬스터 프리팹 (랜덤 선택)
    [SerializeField] private int _epicMaxCount = 2;              // Epic 최대 스폰 수
    [SerializeField] private int _epicInitialCount = 1;          // Epic 초기 스폰 수
    [SerializeField] private float _epicSpawnInterval = 30f;     // Epic 스폰 간격 (초)
    [SerializeField] private int _epicSpawnPerInterval = 1;      // 간격당 Epic 스폰 수

    [Header("Boss 몬스터 설정")]
    [SerializeField] private GameObject[] _bossEnemyPrefabs;     // Boss 몬스터 프리팹 (랜덤 선택)
    [SerializeField] private int _bossMaxCount = 1;              // Boss 최대 스폰 수
    [SerializeField] private int _bossInitialCount = 0;          // Boss 초기 스폰 수 (0 = 주기적으로만 스폰)
    [SerializeField] private float _bossSpawnInterval = 120f;    // Boss 스폰 간격 (초)
    [SerializeField] private int _bossSpawnPerInterval = 1;      // 간격당 Boss 스폰 수

    [Header("Night 몬스터 설정 (저녁 시간대 전용)")]
    [SerializeField] private GameObject[] _nightEnemyPrefabs;   // Night 몬스터 프리팹 (랜덤 선택)
    [SerializeField] private int _nightMaxCount = 3;            // Night 최대 스폰 수
    [SerializeField] private int _nightInitialCount = 2;        // Night 초기 스폰 수
    [SerializeField] private float _nightSpawnInterval = 15f;   // Night 스폰 간격 (초)
    [SerializeField] private int _nightSpawnPerInterval = 1;    // 간격당 Night 스폰 수
    [SerializeField] private float _nightStartHour = 19f;       // 밤 시작 시간 (19시)
    [SerializeField] private float _nightEndHour = 6f;          // 밤 종료 시간 (6시)
    [SerializeField] private float _nightSpawnRadius = 15f;     // 플레이어 주변 스폰 반경
    [SerializeField] private float _nightSpawnMinDistance = 8f; // 최소 스폰 거리 (너무 가까이 스폰 방지)

    [Header("시간 참조")]
    [SerializeField] private GameTimeManager _timeManager;      // 시간 매니저 참조

    [Header("스폰 구역")]
    [SerializeField] private Collider[] _spawnZones;            // 스폰 가능 구역 (BoxCollider, SphereCollider 등 모두 지원)
    
    [Header("스폰 검증")]
    [Tooltip("스폰 가능한 지면 레이어 (Floor)")]
    [SerializeField] private LayerMask _floorLayer;             // Floor 레이어 마스크
    
    [Header("이동 설정")]
    [Tooltip("true: 적이 모든 Zone을 자유롭게 이동 / false: 스폰된 Zone 내에서만 이동")]
    [SerializeField] private bool _allowFreeMovement = true;    // 자유 이동 허용
    
    [Header("NavMesh 최적화")]
    [Tooltip("프레임당 경로 계산 반복 횟수 (높을수록 빠른 경로 생성, CPU 부하 증가)")]
    [Range(100, 1000)]
    [SerializeField] private int _pathfindingIterationsPerFrame = 100;

    [Header("스폰 연출")]
    [Tooltip("각 몬스터 스폰 사이의 딜레이 (초) - 순차 스폰 연출")]
    [Range(0f, 1f)]
    [SerializeField] private float _staggeredSpawnDelay = 0.15f;

    // Zone별 적 관리용 딕셔너리 (Key: Zone, Value: 해당 Zone의 적 리스트)
    private Dictionary<Collider, List<EnemyController>> _zoneEnemies = new Dictionary<Collider, List<EnemyController>>();
    private List<GameObject> _spawnedNormalEnemies = new List<GameObject>(); // Normal 몬스터 목록
    private List<GameObject> _spawnedEpicEnemies = new List<GameObject>();   // Epic 몬스터 목록
    private List<GameObject> _spawnedBossEnemies = new List<GameObject>();   // Boss 몬스터 목록
    private List<GameObject> _spawnedNightEnemies = new List<GameObject>();   // Night 몬스터 목록
    private float _normalSpawnTimer = 0f;  // Normal 스폰 타이머
    private float _epicSpawnTimer = 0f;    // Epic 스폰 타이머
    private float _bossSpawnTimer = 0f;    // Boss 스폰 타이머
    private float _nightSpawnTimer = 0f;   // Night 스폰 타이머
    private bool _wasNight = false;        // 이전 프레임 밤 여부 (낮→밤 전환 감지)
    private float _cleanupTimer = 0f;      // 정리 타이머
    private int _playerZoneCount = 0;      // 플레이어가 진입한 Zone 수
    private Transform _currentPlayer;      // 현재 추적 중인 플레이어
    private bool _hasPlayerEnteredZone = false; // 플레이어 Zone 진입 여부 (스폰 시작 조건)
    private bool _initialSpawnDone = false;     // 초기 스폰 완료 여부 (한 번만 실행)
    private bool _bossSpawnedOnce = false;      // Boss 스폰 완료 (한 번만 스폰)

    private void Start()
    {
        // NavMesh 경로 계산 최적화 (전역 설정)
        NavMesh.pathfindingIterationsPerFrame = _pathfindingIterationsPerFrame;
        
        // 스폰 Zone이 없으면 자식에서 자동 탐색
        if (_spawnZones == null || _spawnZones.Length == 0)
        {
            _spawnZones = GetComponentsInChildren<Collider>();
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
                StartCoroutine(SpawnEnemiesByTypeRoutine(_normalEnemyPrefabs, _normalSpawnPerInterval, _spawnedNormalEnemies, _normalMaxCount));
        }

        // Epic 몬스터 주기적 스폰
        _epicSpawnTimer += Time.deltaTime;
        if (_epicSpawnTimer >= _epicSpawnInterval)
        {
            _epicSpawnTimer = 0f;
            if (_spawnedEpicEnemies.Count < _epicMaxCount)
                StartCoroutine(SpawnEnemiesByTypeRoutine(_epicEnemyPrefabs, _epicSpawnPerInterval, _spawnedEpicEnemies, _epicMaxCount));
        }

        // Boss 몬스터 주기적 스폰 (한 번만 스폰)
        if (!_bossSpawnedOnce)
        {
            _bossSpawnTimer += Time.deltaTime;
            if (_bossSpawnTimer >= _bossSpawnInterval)
            {
                _bossSpawnTimer = 0f;
                if (_spawnedBossEnemies.Count < _bossMaxCount)
                {
                    StartCoroutine(SpawnEnemiesByTypeRoutine(_bossEnemyPrefabs, _bossSpawnPerInterval, _spawnedBossEnemies, _bossMaxCount));
                    _bossSpawnedOnce = true; // 한 번 스폰 후 더 이상 스폰 안 함
                }
            }
        }

        // Night 몬스터 스폰 (밤 시간대만)
        bool isNight = IsNightTime();
        
        // ★ [Debug] 밤 판정 로그 (첫 프레임에만 또는 상태 변경 시)
        if (isNight != _wasNight)
        {
            Debug.Log($"[Night Debug] 시간 전환 감지! isNight={isNight}, currentTime={(_timeManager != null ? _timeManager.currentTime : -1f):F1}");
        }
        
        // 낮→밤 전환 시 초기 스폰
        if (isNight && !_wasNight)
        {
            Debug.Log($"[Night Debug] 밤 시작! Night 몬스터 {_nightInitialCount}마리 초기 스폰 시도...");
            // 밤이 되면 초기 Night 몬스터 스폰 (플레이어 주변)
            SpawnNightEnemies(_nightInitialCount);
            _nightSpawnTimer = 0f;
        }
        
        // 밤→낮 전환 시 Night 몬스터 제거
        if (!isNight && _wasNight)
        {
            DespawnNightEnemies();
        }
        
        _wasNight = isNight;
        
        // 밤 시간대에만 주기적 스폰
        if (isNight)
        {
            _nightSpawnTimer += Time.deltaTime;
            if (_nightSpawnTimer >= _nightSpawnInterval)
            {
                _nightSpawnTimer = 0f;
                SpawnNightEnemies(_nightSpawnPerInterval);
            }
        }
    }
    
    // Night 몬스터 스폰 (플레이어 주변 어디서든, 즉시 추적)
    private void SpawnNightEnemies(int count)
    {
        // 프리팹 배열 유효성 검사
        if (_nightEnemyPrefabs == null || _nightEnemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[Night Debug] Night Enemy Prefabs 배열이 비어있음! Inspector에서 프리팹을 할당하세요.");
            return;
        }
        
        if (_currentPlayer == null)
        {
            // 플레이어 찾기
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _currentPlayer = playerObj.transform;
            if (_currentPlayer == null) return;
        }
        
        for (int i = 0; i < count; i++)
        {
            if (_spawnedNightEnemies.Count >= _nightMaxCount) break;
            
            // 플레이어 주변 랜덤 위치 계산
            Vector3 spawnPos = GetRandomPositionAroundPlayer();
            if (spawnPos == Vector3.zero) continue;
            
            // ★ [수정] 프리팹 배열에서 랜덤 선택
            GameObject selectedPrefab = _nightEnemyPrefabs[Random.Range(0, _nightEnemyPrefabs.Length)];
            if (selectedPrefab == null) continue; // null 프리팹 건너뛰기
            
            // 스폰
            Quaternion lookAtPlayer = Quaternion.LookRotation(_currentPlayer.position - spawnPos);
            GameObject enemyObj = Instantiate(selectedPrefab, spawnPos, lookAtPlayer);
            _spawnedNightEnemies.Add(enemyObj);
            
            // 즉시 플레이어 추적 시작
            EnemyController enemy = enemyObj.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.SetBoundZones(null); // Zone 제한 없음
                enemy.OnPlayerEnterZone(_currentPlayer); // 즉시 추적
            }
        }
    }
    
    // 플레이어 주변 랜덤 위치 (도넛 모양)
    private Vector3 GetRandomPositionAroundPlayer()
    {
        if (_currentPlayer == null) return Vector3.zero;
        
        int maxAttempts = 20; // 시도 횟수 증가
        for (int i = 0; i < maxAttempts; i++)
        {
            // 랜덤 방향, 랜덤 거리
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(_nightSpawnMinDistance, _nightSpawnRadius);
            
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
            Vector3 targetPos = _currentPlayer.position + offset;
            
            // NavMesh 위에서 유효한 위치 찾기
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
            {
                // 플레이어와 경로 연결 검증
                if (CanReachPlayer(hit.position))
                {
                    return hit.position;
                }
            }
        }
        
        return Vector3.zero;
    }
    
    // 플레이어와 NavMesh 경로 연결 검증
    private bool CanReachPlayer(Vector3 spawnPos)
    {
        if (_currentPlayer == null)
        {
            // 플레이어 찾기
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return false;
            _currentPlayer = playerObj.transform;
        }
        
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(spawnPos, _currentPlayer.position, NavMesh.AllAreas, path))
        {
            return path.status == NavMeshPathStatus.PathComplete;
        }
        return false;
    }
    
    // 밤 시간 여부 확인
    private bool IsNightTime()
    {
        if (_timeManager == null)
        {
            Debug.LogWarning("[Night Debug] TimeManager가 할당되지 않음!");
            return false;
        }
        
        float currentHour = _timeManager.currentTime;
        
        // 밤 시작 > 밤 종료 (19시 ~ 6시)
        if (_nightStartHour > _nightEndHour)
        {
            return currentHour >= _nightStartHour || currentHour < _nightEndHour;
        }
        // 밤 시작 < 밤 종료 (예: 22시 ~ 23시)
        else
        {
            return currentHour >= _nightStartHour && currentHour < _nightEndHour;
        }
    }
    
    // Night 몬스터 제거 (낮이 되면 사라짐)
    private void DespawnNightEnemies()
    {
        foreach (var enemy in _spawnedNightEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        _spawnedNightEnemies.Clear();
        Debug.Log("[EnemySpawner] 낮이 되어 Night 몬스터 제거");
    }

    // 유형별 적 스폰 (순차 스폰 코루틴) - 배열에서 랜덤 선택
    private System.Collections.IEnumerator SpawnEnemiesByTypeRoutine(GameObject[] prefabs, int count, List<GameObject> enemyList, int maxCount)
    {
        // 프리팹 배열 유효성 검사
        if (prefabs == null || prefabs.Length == 0) yield break;

        for (int i = 0; i < count; i++)
        {
            if (enemyList.Count >= maxCount) break; // 최대치 도달

            // ★ [수정] 프리팹 배열에서 랜덤 선택
            GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Length)];
            if (selectedPrefab == null) continue; // null 프리팹 건너뛰기

            // 유효한 스폰 위치와 Zone 탐색 (out으로 Zone도 함께 반환)
            Collider selectedZone;
            Vector3 spawnPos = FindValidSpawnPos(out selectedZone);
            
            if (spawnPos != Vector3.zero && selectedZone != null)
            {
                Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f); // Y축 랜덤 회전
                
                GameObject enemyObj = Instantiate(selectedPrefab, spawnPos, randomRotation); // 적 생성
                enemyList.Add(enemyObj); // 유형별 목록에 추가

                // 적에게 Zone 할당 (RestrictToZone 프로퍼티로 분기)
                EnemyController enemy = enemyObj.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    // Epic/Boss: Zone 내 이동 제한 (순찰/감지/추격 순환)
                    // Normal: 자유 이동 (감지 시 끝까지 추적)
                    if (enemy.RestrictToZone)
                    {
                        enemy.SetBoundZones(_spawnZones);
                    }
                    else
                    {
                        enemy.SetBoundZones(null);
                    }
                    _zoneEnemies[selectedZone].Add(enemy);
                    
                    // Normal 몬스터만 즉시 타겟 전달 (Zone 진입 시 돌진)
                    // Epic/Boss 몬스터는 EnemySenses가 감지할 때까지 PatrolState 유지
                    if (_currentPlayer != null && _hasPlayerEnteredZone && !enemy.RestrictToZone)
                    {
                        enemy.OnPlayerEnterZone(_currentPlayer);
                    }
                }
                
                // 순차 스폰 딜레이
                if (_staggeredSpawnDelay > 0 && i < count - 1)
                {
                    yield return new WaitForSeconds(_staggeredSpawnDelay);
                }
            }
        }
    }

    // 유효한 스폰 위치 탐색, out으로 선택된 Zone도 반환
    private Vector3 FindValidSpawnPos(out Collider selectedZone)
    {
        selectedZone = null;
        
        if (_spawnZones == null || _spawnZones.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: 스폰 구역이 설정되지 않았습니다!");
            return Vector3.zero;
        }

        int maxAttempts = 30;       // 최대 시도 횟수
        float searchRadius = 100f;   // NavMesh 탐색 반경 (m)
        
        int navMeshFailCount = 0;   // NavMesh 탐색 실패 횟수
        int boundsFailCount = 0;    // Zone 범위 검증 실패 횟수

        // 최대 시도 횟수만큼 유효한 위치 탐색
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Collider zone = _spawnZones[Random.Range(0, _spawnZones.Length)]; // 랜덤 Zone 선택
            
            // 콜라이더 타입에 맞는 랜덤 포인트 생성
            Vector3 randomPoint = GetRandomPointInCollider(zone);

            // NavMesh 위 유효한 위치 탐색
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, searchRadius, NavMesh.AllAreas))
            {
                // 콜라이더 내부 검증
                if (IsPointInsideCollider(zone, hit.position))
                {
                    // Floor 레이어 검증 + 정확한 바닥 Y 좌표 획득
                    float floorY;
                    if (IsOnFloorLayer(hit.position, out floorY))
                    {
                        // [핵심] Floor Y 좌표로 스폰 위치 보정
                        Vector3 correctedPos = new Vector3(hit.position.x, floorY, hit.position.z);
                        
                        // NavMesh 연결성 검증 (Zone 중심)
                        if (IsNavMeshConnected(correctedPos, zone.bounds.center))
                        {
                            // 플레이어와 경로 연결 검증
                            if (CanReachPlayer(correctedPos))
                            {
                                selectedZone = zone;
                                return correctedPos;
                            }
                        }
                    }
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
        
        return Vector3.zero;
    }
    
    // 콜라이더 타입별 랜덤 포인트 생성
    private Vector3 GetRandomPointInCollider(Collider collider)
    {
        // BoxCollider
        if (collider is BoxCollider box)
        {
            Vector3 localPoint = new Vector3(
                Random.Range(-0.5f, 0.5f) * box.size.x,
                0, // Y는 중앙
                Random.Range(-0.5f, 0.5f) * box.size.z
            );
            return box.transform.TransformPoint(box.center + localPoint);
        }
        
        // SphereCollider
        if (collider is SphereCollider sphere)
        {
            // 구 내부 랜덤 포인트 (2D - XZ 평면)
            Vector2 randomCircle = Random.insideUnitCircle * sphere.radius;
            Vector3 localPoint = new Vector3(randomCircle.x, 0, randomCircle.y);
            return sphere.transform.TransformPoint(sphere.center + localPoint);
        }
        
        // CapsuleCollider
        if (collider is CapsuleCollider capsule)
        {
            float height = capsule.height - capsule.radius * 2; // 원통 부분
            float halfHeight = Mathf.Max(0, height / 2);
            
            // XZ 평면에서 원 내부 랜덤
            Vector2 randomCircle = Random.insideUnitCircle * capsule.radius;
            Vector3 localPoint = new Vector3(randomCircle.x, 0, randomCircle.y);
            return capsule.transform.TransformPoint(capsule.center + localPoint);
        }
        
        // 기타 (MeshCollider 등) - bounds 사용
        Bounds bounds = collider.bounds;
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.center.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }
    
    // 포인트가 콜라이더 내부에 있는지 검증
    private bool IsPointInsideCollider(Collider collider, Vector3 point)
    {
        // Y 좌표는 무시하고 XZ 평면에서 검증
        Vector3 checkPoint = new Vector3(point.x, collider.bounds.center.y, point.z);
        
        // BoxCollider
        if (collider is BoxCollider box)
        {
            Vector3 localPoint = box.transform.InverseTransformPoint(checkPoint);
            Vector3 halfSize = box.size * 0.5f;
            Vector3 offset = localPoint - box.center;
            
            return Mathf.Abs(offset.x) <= halfSize.x && Mathf.Abs(offset.z) <= halfSize.z;
        }
        
        // SphereCollider
        if (collider is SphereCollider sphere)
        {
            Vector3 worldCenter = sphere.transform.TransformPoint(sphere.center);
            float radiusWorld = sphere.radius * Mathf.Max(sphere.transform.lossyScale.x, sphere.transform.lossyScale.z);
            float distXZ = Vector2.Distance(new Vector2(checkPoint.x, checkPoint.z), new Vector2(worldCenter.x, worldCenter.z));
            return distXZ <= radiusWorld;
        }
        
        // CapsuleCollider
        if (collider is CapsuleCollider capsule)
        {
            Vector3 worldCenter = capsule.transform.TransformPoint(capsule.center);
            float radiusWorld = capsule.radius * Mathf.Max(capsule.transform.lossyScale.x, capsule.transform.lossyScale.z);
            float distXZ = Vector2.Distance(new Vector2(checkPoint.x, checkPoint.z), new Vector2(worldCenter.x, worldCenter.z));
            return distXZ <= radiusWorld;
        }
        
        // 기타 - bounds 사용
        return collider.bounds.Contains(checkPoint);
    }
    
    // Floor 레이어 검증 - 스폰 위치 위에서 아래로 Raycast, Floor 레이어만 검색
    // [수정] out floorY: Floor와 닿은 위치의 Y 좌표 반환 (정확한 바닥 높이)
    private bool IsOnFloorLayer(Vector3 position, out float floorY, float rayStartHeight = 50f)
    {
        floorY = position.y; // 기본값
        
        // 레이어 마스크가 설정되지 않았으면 통과 (폴백)
        if (_floorLayer == 0)
        {
            Debug.LogWarning($"[Spawn] {gameObject.name}: Floor Layer가 설정되지 않음! Inspector에서 설정하세요.");
            return true;
        }
        
        // [핵심 수정] 충분히 높은 위치에서 Floor 레이어만 대상으로 Raycast
        Vector3 rayStart = new Vector3(position.x, position.y + rayStartHeight, position.z);
        Ray ray = new Ray(rayStart, Vector3.down);
        float rayDistance = rayStartHeight + 50f;
        
        // Floor 레이어만 검색 (다른 콜라이더 무시) + Trigger도 포함
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, _floorLayer, QueryTriggerInteraction.Ignore))
        {
            // Floor와 닿은 정확한 Y 좌표 반환
            floorY = hit.point.y;
            return true;
        }
        
        // Floor에 안맞으면 - 디버그
        Debug.LogWarning($"[Floor MISS] {gameObject.name}: Raycast 실패! RayStart=({rayStart.x:F1}, {rayStart.y:F1}, {rayStart.z:F1}), Distance={rayDistance:F1}, LayerMask={_floorLayer.value}");
        return false;
    }
    
    // NavMesh 연결성 검증 - 스폰 위치에서 목표 지점까지 경로가 있는지 확인
    private bool IsNavMeshConnected(Vector3 spawnPos, Vector3 targetCenter)
    {
        // 목표 지점의 NavMesh 위치 찾기
        NavMeshHit centerHit;
        if (!NavMesh.SamplePosition(targetCenter, out centerHit, 50f, NavMesh.AllAreas))
        {
            return true; // Zone 중심에 NavMesh가 없으면 그냥 통과
        }
        
        // 스폰 위치 → Zone 중심 경로 계산
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(spawnPos, centerHit.position, NavMesh.AllAreas, path))
        {
            // 경로가 완전히 연결되어야 함
            return path.status == NavMeshPathStatus.PathComplete;
        }
        
        return false;
    }

    // 플레이어 Zone 진입 시 호출 (EnemyZoneTrigger에서 호출)
    public void OnPlayerEnterAnyZone(Transform player)
    {
        _playerZoneCount++;         // Zone 카운트 증가
        _currentPlayer = player;    // 현재 플레이어 저장
        _hasPlayerEnteredZone = true; // 스폰 활성화
        
        // 스폰 타이머 리셋 (재입장 시 인터벌 스폰 즉시 실행 방지)
        _normalSpawnTimer = 0f;
        _epicSpawnTimer = 0f;
        _bossSpawnTimer = 0f;
        
        // 초기 스폰 (최초 한 번만)
        if (!_initialSpawnDone)
        {
            _initialSpawnDone = true;
            StartCoroutine(SpawnEnemiesByTypeRoutine(_normalEnemyPrefabs, _normalInitialCount, _spawnedNormalEnemies, _normalMaxCount));
            StartCoroutine(SpawnEnemiesByTypeRoutine(_epicEnemyPrefabs, _epicInitialCount, _spawnedEpicEnemies, _epicMaxCount));
            
            // Boss 초기 스폰 (설정된 경우)
            if (_bossInitialCount > 0 && !_bossSpawnedOnce)
            {
                StartCoroutine(SpawnEnemiesByTypeRoutine(_bossEnemyPrefabs, _bossInitialCount, _spawnedBossEnemies, _bossMaxCount));
                _bossSpawnedOnce = true; // Boss는 한 번만 스폰
            }
        }
        
        NotifyAllEnemiesEnter(player); // 모든 적에게 알림
    }

    // 플레이어 Zone 퇴장 시 호출 (EnemyZoneTrigger에서 호출)
    public void OnPlayerExitZone(Collider exitedZone, Transform player)
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
            _hasPlayerEnteredZone = false; // 스폰 중단
            _currentPlayer = null;
            
            // 스폰 타이머 리셋 (재입장 시 바로 스폰 방지)
            _normalSpawnTimer = 0f;
            _epicSpawnTimer = 0f;
            _bossSpawnTimer = 0f;
            
            // 재입장 시 초기 스폰 다시 실행되도록 리셋
            _initialSpawnDone = false;
            
            NotifyAllEnemiesExit();
        }
    }

    // 모든 적에게 플레이어 진입 알림
    private void NotifyAllEnemiesEnter(Transform player)
    {
        NotifyEnemyList(_spawnedNormalEnemies, player, true);
        NotifyEnemyList(_spawnedEpicEnemies, player, true);
        NotifyEnemyList(_spawnedBossEnemies, player, true);
        NotifyEnemyList(_spawnedNightEnemies, player, true);
    }

    // 모든 적에게 플레이어 퇴장 알림
    private void NotifyAllEnemiesExit()
    {
        NotifyEnemyList(_spawnedNormalEnemies, null, false);
        NotifyEnemyList(_spawnedEpicEnemies, null, false);
        NotifyEnemyList(_spawnedBossEnemies, null, false);
        NotifyEnemyList(_spawnedNightEnemies, null, false);
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
                // 모든 몬스터에게 Zone 진입 알림 (IsPlayerInZone 설정)
                controller.OnPlayerEnterZone(player);
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
        _spawnedBossEnemies.RemoveAll(enemy => enemy == null);   // Boss 목록에서 null 제거
        _spawnedNightEnemies.RemoveAll(enemy => enemy == null);  // Night 목록에서 null 제거

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
        List<Collider> drawList = new List<Collider>();
        
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
            drawList.AddRange(GetComponentsInChildren<Collider>());
        }

        // 3. 그리기
        foreach (Collider zone in drawList)
        {
            if (zone == null) continue;

            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // 반투명 녹색

            // BoxCollider
            if (zone is BoxCollider box)
            {
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(0f, 1f, 0f, 1f);
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            // SphereCollider
            else if (zone is SphereCollider sphere)
            {
                Vector3 worldCenter = sphere.transform.TransformPoint(sphere.center);
                float worldRadius = sphere.radius * Mathf.Max(sphere.transform.lossyScale.x, sphere.transform.lossyScale.y, sphere.transform.lossyScale.z);
                Gizmos.DrawSphere(worldCenter, worldRadius);
                Gizmos.color = new Color(0f, 1f, 0f, 1f);
                Gizmos.DrawWireSphere(worldCenter, worldRadius);
            }
            // CapsuleCollider
            else if (zone is CapsuleCollider capsule)
            {
                Vector3 worldCenter = capsule.transform.TransformPoint(capsule.center);
                float worldRadius = capsule.radius * Mathf.Max(capsule.transform.lossyScale.x, capsule.transform.lossyScale.z);
                Gizmos.DrawSphere(worldCenter, worldRadius);
                Gizmos.color = new Color(0f, 1f, 0f, 1f);
                Gizmos.DrawWireSphere(worldCenter, worldRadius);
            }
            // 기타 - bounds 사용
            else
            {
                Gizmos.DrawCube(zone.bounds.center, zone.bounds.size);
                Gizmos.color = new Color(0f, 1f, 0f, 1f);
                Gizmos.DrawWireCube(zone.bounds.center, zone.bounds.size);
            }
        }
        
        // Night Enemy 스폰 범위 시각화 (도넛 모양)
        DrawNightSpawnRange();
    }
    
    // Night 스폰 범위 도넛 시각화
    private void DrawNightSpawnRange()
    {
        // 플레이어 위치 찾기
        Vector3 playerPos = Vector3.zero;
        
        if (_currentPlayer != null)
        {
            playerPos = _currentPlayer.position;
        }
        else
        {
            // 에디터에서 Player 태그로 찾기
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerPos = playerObj.transform.position;
            }
            else
            {
                return; // 플레이어 없으면 안 그림
            }
        }

        // [New] 플레이어가 스폰 구역 내부에 있을 때만 그리기
        bool isInsideZone = false;
        if (_spawnZones != null)
        {
            foreach (var zone in _spawnZones)
            {
                if (zone != null && IsPointInsideCollider(zone, playerPos))
                {
                    isInsideZone = true;
                    break;
                }
            }
        }

        if (!isInsideZone) return; // 구역 밖이면 시각화 안 함
        
        // (선 제거됨) 내부 채우기 그리기 제거
        
        // 외곽선만 그리기 (깔끔하게)
        Gizmos.color = new Color(0.5f, 0f, 1f, 1f); // 보라색
        DrawWireCircle(playerPos, _nightSpawnRadius, 32);
        
        Gizmos.color = new Color(1f, 0f, 0.5f, 1f); // 분홍색
        DrawWireCircle(playerPos, _nightSpawnMinDistance, 32);
        
        // 라벨 표시용 작은 구
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(playerPos + Vector3.forward * _nightSpawnRadius, 0.3f);
        Gizmos.DrawWireSphere(playerPos + Vector3.forward * _nightSpawnMinDistance, 0.3f);
    }
    
    // 원 그리기 (외곽선)
    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * 360f * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
#endif
}
