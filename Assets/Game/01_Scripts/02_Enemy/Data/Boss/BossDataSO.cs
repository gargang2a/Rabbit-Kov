using UnityEngine;

/// <summary>
/// [Role] 보스 데이터 ScriptableObject
/// 보스 스탯, 페이즈 설정, 공격 패턴 참조
/// </summary>
[CreateAssetMenu(fileName = "BossData_New", menuName = "Rabbit-Kov/Boss/Boss Data")]
public class BossDataSO : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("보스 이름")]
    public string bossName = "New Boss";
    
    [TextArea(2, 4)]
    public string description;

    [Header("스탯")]
    [Tooltip("최대 체력")]
    [Range(1000, 100000)]
    public int maxHealth = 10000;
    
    [Tooltip("이동 속도")]
    [Range(1f, 10f)]
    public float moveSpeed = 3f;
    
    [Tooltip("회전 속도")]
    [Range(1f, 20f)]
    public float rotationSpeed = 5f;

    [Header("페이즈 설정")]
    [Tooltip("페이즈 2 전환 체력 비율 (0~1)")]
    [Range(0.1f, 0.9f)]
    public float phase2Threshold = 0.66f;
    
    [Tooltip("페이즈 3 전환 체력 비율 (0~1)")]
    [Range(0.1f, 0.9f)]
    public float phase3Threshold = 0.33f;

    [Header("페이즈별 공격")]
    [Tooltip("1페이즈 공격 프리팹")]
    public GameObject phase1AttackPrefab;
    
    [Tooltip("2페이즈 공격 프리팹")]
    public GameObject phase2AttackPrefab;
    
    [Tooltip("3페이즈 공격 프리팹")]
    public GameObject phase3AttackPrefab;

    [Header("보상")]
    [Tooltip("처치 시 경험치")]
    public int expReward = 1000;
    
    [Tooltip("드롭 아이템")]
    public ScriptableObject lootTable;
}
