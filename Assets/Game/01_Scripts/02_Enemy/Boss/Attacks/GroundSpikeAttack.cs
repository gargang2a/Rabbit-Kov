using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// [역할] 2페이즈 공격 - 바닥 스파이크 (3갈래로 뻗어나감)
public class GroundSpikeAttack : MonoBehaviour, IBossAttack
{
    [Header("공격 설정")]
    [SerializeField] private string _attackName = "바닥 스파이크";
    [SerializeField] private float _cooldown = 4f;
    
    [Header("스파이크 설정")]
    [Tooltip("스파이크 갈래 수")]
    [SerializeField] private int _spikeCount = 3;
    
    [Tooltip("스파이크 최대 거리")]
    [SerializeField] private float _maxDistance = 15f;
    
    [Tooltip("스파이크 이동 속도")]
    [SerializeField] private float _spikeSpeed = 10f;
    
    [Tooltip("스파이크 간 각도 (도)")]
    [SerializeField] private float _spreadAngle = 30f;
    
    [Tooltip("데미지")]
    [SerializeField] private int _damage = 80;
    
    [Tooltip("선딜레이 (초)")]
    [SerializeField] private float _windupTime = 0.8f;
    
    [Header("프리팹")]
    [Tooltip("스파이크 프리팹")]
    [SerializeField] private GameObject _spikePrefab;
    
    [Tooltip("경고 라인 프리팹")]
    [SerializeField] private GameObject _warningLinePrefab;
    
    private bool _isExecuting = false;
    private Coroutine _attackCoroutine;
    private List<GameObject> _activeSpikes = new List<GameObject>();
    
    public string AttackName => _attackName;
    public float Cooldown => _cooldown;
    public bool IsExecuting => _isExecuting;

    public void Execute(BossController boss, Transform target)
    {
        if (_isExecuting) return;
        
        _attackCoroutine = StartCoroutine(ExecuteAttackRoutine(boss, target));
    }

    public void Cancel()
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }
        
        // 활성 스파이크 정리
        foreach (var spike in _activeSpikes)
        {
            if (spike != null) Destroy(spike);
        }
        _activeSpikes.Clear();
        
        _isExecuting = false;
    }

    private IEnumerator ExecuteAttackRoutine(BossController boss, Transform target)
    {
        _isExecuting = true;
        Vector3 bossPos = boss.transform.position;
        Vector3 targetPos = target.position;
        
        // 메인 방향 (보스 → 플레이어)
        Vector3 mainDirection = (targetPos - bossPos).normalized;
        mainDirection.y = 0; // 수평만
        
        // 각 갈래 방향 계산
        List<Vector3> spikeDirections = new List<Vector3>();
        float startAngle = -(_spikeCount - 1) * _spreadAngle / 2f;
        
        for (int i = 0; i < _spikeCount; i++)
        {
            float angle = startAngle + i * _spreadAngle;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * mainDirection;
            spikeDirections.Add(dir);
        }

        // 1. 선딜레이 (경고 라인)
        List<GameObject> warnings = new List<GameObject>();
        if (_warningLinePrefab != null)
        {
            foreach (var dir in spikeDirections)
            {
                GameObject warning = Instantiate(_warningLinePrefab, bossPos, Quaternion.LookRotation(dir));
                warning.transform.localScale = new Vector3(1, 1, _maxDistance);
                warnings.Add(warning);
            }
        }
        
        yield return new WaitForSeconds(_windupTime);
        
        foreach (var w in warnings) // 경고 제거
        {
            if (w != null) Destroy(w);
        }

        // 2. 스파이크 발사
        foreach (var dir in spikeDirections)
        {
            StartCoroutine(LaunchSpikeLine(bossPos, dir));
        }

        // 스파이크 끝날 때까지 대기
        float duration = _maxDistance / _spikeSpeed;
        yield return new WaitForSeconds(duration + 0.5f);

        _isExecuting = false;
    }

    private IEnumerator LaunchSpikeLine(Vector3 startPos, Vector3 direction)
    {
        float traveled = 0f;
        float spawnInterval = 1f; // 스파이크 간격 (m)
        float lastSpawnDist = 0f;

        while (traveled < _maxDistance)
        {
            traveled += _spikeSpeed * Time.deltaTime;
            
            if (traveled - lastSpawnDist >= spawnInterval) // 간격마다 생성
            {
                lastSpawnDist = traveled;
                Vector3 spawnPos = startPos + direction * traveled;
                spawnPos.y = 0; // 지면
                
                if (_spikePrefab != null)
                {
                    GameObject spike = Instantiate(_spikePrefab, spawnPos, Quaternion.identity);
                    _activeSpikes.Add(spike);
                    Destroy(spike, 1.5f); // 자동 파괴
                    CheckSpikeDamage(spawnPos);
                }
            }
            
            yield return null;
        }
    }

    private void CheckSpikeDamage(Vector3 position)
    {
        float damageRadius = 1f;
        Collider[] hits = Physics.OverlapSphere(position, damageRadius);
        
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log($"[GroundSpike] 플레이어 적중! (Damage: {_damage})");
            }
        }
    }
}
