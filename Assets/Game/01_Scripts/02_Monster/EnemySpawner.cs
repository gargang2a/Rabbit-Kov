using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ============================================================================
// EnemySpawner - 적 스폰 시스템
// ============================================================================
// 
// [역할]
// 적을 지정된 구역 내에 자동으로 생성하는 컴포넌트입니다.
// 게임 시작 시 초기 적을 배치하고, 일정 간격으로 추가 적을 생성합니다.
// 
// [스폰 규칙]
// 1. 최대 적 수(_maxEnemyCount)를 초과하지 않음
// 2. NavMesh 위의 유효한 위치에만 스폰
// 3. 스폰 구역(이 오브젝트의 Scale) 내에서 랜덤 위치 선택
// 
// [스폰 구역 설정]
// - 이 오브젝트의 Position: 스폰 구역 중심
// - 이 오브젝트의 Scale: 스폰 구역 크기
// - _useBoxArea: true면 사각형, false면 원형 구역
// 
// [사용법]
// 1. 빈 게임오브젝트 생성
// 2. EnemySpawner 컴포넌트 추가
// 3. _enemyPrefab에 적 프리팹 할당
// 4. Transform의 Scale로 스폰 구역 크기 조절
// 5. Scene 뷰에서 빨간색 영역으로 확인 가능
// ============================================================================
public class EnemySpawner : MonoBehaviour
{
    // ==================== 스폰 설정 ====================
    
    [Header("스폰 설정")]
    
    // 생성할 적의 프리팹. EnemyController가 포함되어 있어야 합니다.
    [SerializeField] private GameObject _enemyPrefab;
    
    // 동시에 존재할 수 있는 최대 적 수. 이 수를 초과하면 스폰하지 않습니다.
    [SerializeField] private int _maxEnemyCount = 5;
    
    // 게임 시작 시 즉시 생성할 적의 수
    [SerializeField] private int _initialSpawnCount = 3;
    
    // 적 생성 간격 (초 단위). 이 시간마다 적을 추가 생성합니다.
    [SerializeField] private float _spawnInterval = 10f;
    
    // 한 번에 생성할 적의 수 (스폰 간격마다 이 숫자만큼 생성)
    [SerializeField] private int _spawnPerInterval = 1;

    // ==================== 스폰 구역 설정 ====================
    
    [Header("스폰 구역 (이 오브젝트의 Scale 사용)")]
    
    // true: 사각형(Box) 구역, false: 원형(Circle) 구역
    // Scale의 X, Z 값이 각각 가로, 세로 길이가 됨
    [SerializeField] private bool _useBoxArea = true;

    // ==================== 런타임 변수 ====================
    
    // 현재 스폰된 적들의 목록. 죽은 적 정리와 최대 수 체크에 사용합니다.
    private List<GameObject> _spawnedEnemies = new List<GameObject>();
    
    // 다음 스폰까지의 시간을 측정하는 타이머
    private float _spawnTimer = 0f;

    // ==================== MonoBehaviour 생명주기 ====================
    
    // Start: 게임 시작 시 초기 적 배치
    private void Start()
    {
        SpawnInitialEnemies();
    }

    // Update: 매 프레임 스폰 로직 실행
    private void Update()
    {
        // 1. 죽은 적 정리 (null 체크)
        CleanupDeadEnemies();

        // 2. 최대 수 체크 (이미 최대면 스폰하지 않음)
        if (_spawnedEnemies.Count >= _maxEnemyCount) return;

        // 3. 스폰 타이머 증가
        _spawnTimer += Time.deltaTime;

        // 4. 스폰 간격 도달 시 적 생성
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;  // 타이머 리셋
            SpawnEnemies(_spawnPerInterval);
        }
    }

    // ==================== 스폰 메서드 ====================
    
    // 게임 시작 시 초기 적 배치
    private void SpawnInitialEnemies()
    {
        SpawnEnemies(_initialSpawnCount);
    }

    // 지정된 수만큼 적 생성
    // count: 생성할 적의 수
    private void SpawnEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // 최대 수 초과 방지
            if (_spawnedEnemies.Count >= _maxEnemyCount) break;

            // 랜덤 스폰 위치 계산
            Vector3 spawnPosition = GetRandomSpawnPosition();

            // 유효한 위치를 찾았으면 적 생성
            // Vector3.zero는 위치를 찾지 못했다는 의미
            if (spawnPosition != Vector3.zero)
            {
                // 랜덤 Y축 회전 생성 (0~360도)
                // 각 적이 다른 방향을 바라보게 하여 초기 이동 시 분산되도록 함
                float randomYRotation = Random.Range(0f, 360f);
                Quaternion randomRotation = Quaternion.Euler(0f, randomYRotation, 0f);
                
                // Instantiate: 프리팹을 복제하여 새 게임오브젝트 생성
                GameObject enemy = Instantiate(_enemyPrefab, spawnPosition, randomRotation);
                _spawnedEnemies.Add(enemy);
            }
        }
    }

    // NavMesh 위의 랜덤한 스폰 위치 계산
    // 반환값: 유효한 NavMesh 위치, 실패 시 Vector3.zero
    private Vector3 GetRandomSpawnPosition()
    {
        // 스폰 구역의 중심 (이 오브젝트의 위치)
        Vector3 center = transform.position;
        // 스폰 구역의 크기 (이 오브젝트의 스케일)
        Vector3 size = transform.localScale;

        // 최대 시도 횟수 (무한 루프 방지)
        int maxAttempts = 30;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 randomPoint;

            if (_useBoxArea)
            {
                // ===== 사각형 구역 =====
                // Random.Range: 지정 범위 내 랜덤 값 반환
                // center ± size/2 범위에서 랜덤 좌표 생성
                randomPoint = new Vector3(
                    center.x + Random.Range(-size.x / 2f, size.x / 2f),
                    center.y,  // Y는 스폰어 높이 유지
                    center.z + Random.Range(-size.z / 2f, size.z / 2f)
                );
            }
            else
            {
                // ===== 원형 구역 =====
                // X, Z 중 큰 값을 반지름으로 사용
                float radius = Mathf.Max(size.x, size.z) / 2f;
                // insideUnitCircle: 반지름 1인 원 내부의 랜덤 2D 좌표
                Vector2 randomCircle = Random.insideUnitCircle * radius;
                randomPoint = new Vector3(
                    center.x + randomCircle.x,
                    center.y,
                    center.z + randomCircle.y  // 2D의 Y → 3D의 Z
                );
            }

            // ===== NavMesh 위치 확인 =====
            // SamplePosition: 지정 위치 근처에서 NavMesh 위의 유효한 위치를 찾음
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
            {
                // Y 높이 차이 계산 (스폰어 높이와 NavMesh 위치의 차이)
                float heightDifference = Mathf.Abs(hit.position.y - transform.position.y);
                
                // 허용 오차: 0.25m 이내의 높이 차이만 허용
                // 이 범위를 벗어나면 다른 층이나 경사면으로 판단하여 스킵
                float heightTolerance = 0.25f;
                
                if (heightDifference > heightTolerance)
                {
                    continue; // 높이가 다르면 다시 시도
                }
                
                // 스폰어와 같은 Y 높이로 고정하여 반환
                return new Vector3(hit.position.x, transform.position.y, hit.position.z);
            }
        }

        // 유효한 위치를 찾지 못함
        Debug.LogWarning("EnemySpawner: NavMesh 위치를 찾지 못했습니다.");
        return Vector3.zero;
    }

    // 파괴된 적(null)을 목록에서 제거
    // 적이 죽으면 GameObject가 Destroy되어 null이 됨
    private void CleanupDeadEnemies()
    {
        // RemoveAll: 조건을 만족하는 모든 요소 제거
        // Lambda: enemy => enemy == null (null인 항목 제거)
        _spawnedEnemies.RemoveAll(enemy => enemy == null);
    }

// ==================== 에디터 전용: 시각적 디버깅 ====================
#if UNITY_EDITOR
    // OnDrawGizmos: Scene 뷰에서 스폰 구역 시각화
    private void OnDrawGizmos()
    {
        // 반투명 빨간색으로 스폰 구역 내부 표시
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        if (_useBoxArea)
        {
            // 사각형: DrawCube로 채워진 박스 그리기
            Gizmos.DrawCube(transform.position, transform.localScale);
        }
        else
        {
            // 원형: DrawSphere로 채워진 구 그리기
            float radius = Mathf.Max(transform.localScale.x, transform.localScale.z) / 2f;
            Gizmos.DrawSphere(transform.position, radius);
        }

        // 빨간색 테두리로 스폰 구역 경계 표시
        Gizmos.color = Color.red;
        if (_useBoxArea)
        {
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
        else
        {
            float radius = Mathf.Max(transform.localScale.x, transform.localScale.z) / 2f;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
#endif
}

