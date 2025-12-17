using UnityEngine;

/// <summary>
/// [Role] 적 데이터 ScriptableObject - 모든 적 설정을 통합
/// 스탯, 이동, 감지, 공격 데이터를 하나로 관리
/// </summary>
[CreateAssetMenu(fileName = "EnemyData_New", menuName = "Rabbit-Kov/Enemy/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("적 이름 (디버깅/UI용)")]
    public string enemyName = "New Enemy";
    
    [Tooltip("적 티어")]
    public EnemyTier tier = EnemyTier.Normal;
    
    [TextArea(2, 4)]
    public string description;

    [Header("스탯")]
    [Tooltip("최대 체력")]
    [Range(1, 10000)]
    public int maxHealth = 100;
    
    [Tooltip("피격 시 넉백 저항 (높을수록 덜 밀림)")]
    [Range(0f, 1f)]
    public float knockbackResistance = 0f;

    [Header("이동")]
    [Tooltip("기본 이동 속도")]
    [Range(0.5f, 20f)]
    public float moveSpeed = 3.5f;
    
    [Tooltip("추적 시 이동 속도")]
    [Range(0.5f, 25f)]
    public float chaseSpeed = 5f;
    
    [Tooltip("회전 속도")]
    [Range(1f, 20f)]
    public float rotationSpeed = 10f;
    
    [Tooltip("정찰 범위 (최대 거리)")]
    [Range(1f, 30f)]
    public float patrolRadius = 10f;

    [Header("감지")]
    [Tooltip("시야 반경")]
    [Range(1f, 50f)]
    public float sightRadius = 10f;
    
    [Tooltip("시야각 (도)")]
    [Range(30f, 360f)]
    public float fieldOfView = 120f;
    
    [Tooltip("타겟 소실 시간 (초) - 이 시간 후 타겟 해제")]
    [Range(0.5f, 10f)]
    public float targetLostTimeout = 3f;

    // ========== 공격 설정 ==========
    
    [Header("공격 (최대 4가지)")]
    [Tooltip("Default: 부딪히면 발생하는 기본 접촉 공격")]
    public GameObject defaultAttackPrefab;
    
    [Tooltip("Phase 1: 1페이즈 공격 (또는 Normal/Epic의 주 공격)")]
    public GameObject phase1AttackPrefab;
    
    [Tooltip("Phase 2: 2페이즈 공격 (Boss 전용)")]
    public GameObject phase2AttackPrefab;
    
    [Tooltip("Phase 3: 3페이즈 공격 (Boss 전용)")]
    public GameObject phase3AttackPrefab;

    [Header("AI 행동")]
    [Tooltip("Zone 내 이동 제한 (Epic/Boss용)")]
    public bool restrictToZone = false;
    
    [Tooltip("무한 추적 (Normal용)")]
    public bool infiniteChase = true;

    [Header("보상")]
    [Tooltip("처치 시 경험치")]
    public int expReward = 10;
    
    [Tooltip("드롭 테이블 (선택)")]
    public ScriptableObject lootTable;

    // ========== 보스 전용 설정 (tier = Boss) ==========
    
    [Header("보스 전용 (tier = Boss일 때만 유효)")]
    [Tooltip("페이즈 2 전환 체력 비율 (0~1)")]
    [Range(0.1f, 0.9f)]
    public float phase2Threshold = 0.66f;
    
    [Tooltip("페이즈 3 전환 체력 비율 (0~1)")]
    [Range(0.1f, 0.9f)]
    public float phase3Threshold = 0.33f;

    // ========== 계산 프로퍼티 ==========
    
    /// <summary>티어에 따른 체력 배율</summary>
    public float TierHealthMultiplier => tier switch
    {
        EnemyTier.Normal => 1f,
        EnemyTier.Epic => 2f,
        EnemyTier.Boss => 5f,
        _ => 1f
    };
    
    /// <summary>티어에 따른 데미지 배율</summary>
    public float TierDamageMultiplier => tier switch
    {
        EnemyTier.Normal => 1f,
        EnemyTier.Epic => 1.5f,
        EnemyTier.Boss => 2f,
        _ => 1f
    };
    
    /// <summary>최종 체력 (티어 배율 적용)</summary>
    public int FinalMaxHealth => Mathf.RoundToInt(maxHealth * TierHealthMultiplier);
}
