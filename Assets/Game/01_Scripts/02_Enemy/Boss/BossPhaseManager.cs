using UnityEngine;
using System;

/// <summary>
/// [Role] 보스 페이즈 관리자
/// 체력에 따른 페이즈 전환 및 공격 패턴 선택
/// EnemyDataSO의 보스 필드 사용
/// </summary>
public class BossPhaseManager : MonoBehaviour
{
    [Header("페이즈 설정")]
    [SerializeField] private int _currentPhase = 1;
    
    // 이벤트
    public event Action<int> OnPhaseChanged; // 페이즈 변경 시 (새 페이즈 번호)
    
    // 참조
    private BossController _boss;
    private EnemyDataSO _enemyData; // BossDataSO → EnemyDataSO
    
    // 프로퍼티
    public int CurrentPhase => _currentPhase;
    public int MaxPhase => 3;

    /// <summary>
    /// 초기화 (EnemyDataSO 사용)
    /// </summary>
    public void Initialize(BossController boss, EnemyDataSO data)
    {
        _boss = boss;
        _enemyData = data;
        _currentPhase = 1;
    }

    /// <summary>
    /// 체력 변화 시 호출 - 페이즈 전환 체크
    /// </summary>
    /// <param name="healthRatio">현재 체력 비율 (0~1)</param>
    public void CheckPhaseTransition(float healthRatio)
    {
        int newPhase = CalculatePhase(healthRatio);
        
        if (newPhase != _currentPhase)
        {
            int oldPhase = _currentPhase;
            _currentPhase = newPhase;
            
            Debug.Log($"[Boss] {_boss.name}: 페이즈 {oldPhase} → {newPhase} (체력: {healthRatio:P0})");
            OnPhaseChanged?.Invoke(_currentPhase);
        }
    }

    /// <summary>
    /// 체력 비율에 따른 페이즈 계산
    /// </summary>
    private int CalculatePhase(float healthRatio)
    {
        if (_enemyData == null) return 1;
        
        if (healthRatio <= _enemyData.phase3Threshold)
            return 3;
        else if (healthRatio <= _enemyData.phase2Threshold)
            return 2;
        else
            return 1;
    }

    /// <summary>
    /// 현재 페이즈의 공격 프리팹 반환
    /// </summary>
    public GameObject GetCurrentAttackPrefab()
    {
        if (_enemyData == null) return null;
        
        return _currentPhase switch
        {
            1 => _enemyData.phase1AttackPrefab,
            2 => _enemyData.phase2AttackPrefab,
            3 => _enemyData.phase3AttackPrefab,
            _ => _enemyData.phase1AttackPrefab
        };
    }
}
