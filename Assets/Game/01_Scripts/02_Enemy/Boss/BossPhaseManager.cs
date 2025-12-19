using UnityEngine;
using System;

// [역할] 보스 페이즈 관리자 - 체력 기반 페이즈 전환 및 공격 패턴 선택
public class BossPhaseManager : MonoBehaviour
{
    [Header("페이즈 설정")]
    [SerializeField] private int _currentPhase = 1;
    
    public event Action<int> OnPhaseChanged; // 페이즈 변경 이벤트
    
    private BossController _boss;     // 보스 참조
    private EnemyDataSO _enemyData;   // 데이터
    
    public int CurrentPhase => _currentPhase;
    public int MaxPhase => 3;

    // 초기화
    public void Initialize(BossController boss, EnemyDataSO data)
    {
        _boss = boss;
        _enemyData = data;
        _currentPhase = 1;
    }

    // 페이즈 전환 체크
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

    // 체력 비율로 페이즈 계산
    private int CalculatePhase(float healthRatio)
    {
        if (_enemyData == null) return 1;
        
        if (healthRatio <= _enemyData.phase3Threshold)
        {
            return 3;
        }
        else if (healthRatio <= _enemyData.phase2Threshold)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }

    // 현재 페이즈 공격 데이터 반환
    public EnemyAttackDataSO GetCurrentAttackData()
    {
        if (_enemyData == null) return null;
        
        if (_currentPhase == 1)
        {
            return _enemyData.phase1Attack;
        }
        else if (_currentPhase == 2)
        {
            return _enemyData.phase2Attack;
        }
        else if (_currentPhase == 3)
        {
            return _enemyData.phase3Attack;
        }
        else
        {
            return _enemyData.phase1Attack;
        }
    }
    
    // 현재 페이즈 공격 프리팹 반환
    public GameObject GetCurrentAttackPrefab()
    {
        EnemyAttackDataSO attackData = GetCurrentAttackData();
        return attackData?.attackPrefab;
    }
}
