using UnityEngine;

// [역할] 공격 데이터 ScriptableObject - 공격 패턴 설정
[CreateAssetMenu(fileName = "EnemyAttack_New", menuName = "Rabbit-Kov/Combat/Enemy Attack Data")]
public class EnemyAttackDataSO : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("공격 이름 (디버깅/UI용)")]
    public string attackName = "New Attack";
    
    [Tooltip("공격 설명")]
    [TextArea(2, 4)]
    public string description;
    
    [Tooltip("공격 실행 프리팹 (IBossAttack 구현)")]
    public GameObject attackPrefab;

    [Header("데미지 설정")]
    [Tooltip("기본 데미지")]
    [Range(1, 1000)]
    public int baseDamage = 10;
    
    [Tooltip("크리티컬 확률 (0~1)")]
    [Range(0f, 1f)]
    public float criticalChance = 0f;
    
    [Tooltip("크리티컬 배율")]
    [Range(1f, 5f)]
    public float criticalMultiplier = 2f;

    [Header("사거리 설정")]
    [Tooltip("공격 사거리 (m)")]
    [Range(0.5f, 20f)]
    public float attackRange = 1.5f;
    
    [Tooltip("공격 각도 (도) - 180이면 전방 반원")]
    [Range(0f, 360f)]
    public float attackAngle = 90f;

    [Header("타이밍 설정")]
    [Tooltip("선딜 (Windup) 시간 (초)")]
    [Range(0f, 5f)]
    public float windupDuration = 0.3f;
    
    [Tooltip("공격 지속 시간 (초)")]
    [Range(0.1f, 5f)]
    public float attackDuration = 0.2f;
    
    [Tooltip("후딜 (Recovery) 시간 (초)")]
    [Range(0f, 5f)]
    public float recoveryDuration = 0.5f;
    
    [Tooltip("쿨다운 (초)")]
    [Range(0f, 30f)]
    public float cooldown = 1f;

    [Header("넉백 설정")]
    [Tooltip("넉백 강도")]
    [Range(0f, 50f)]
    public float knockbackForce = 5f;
    
    [Tooltip("넉백 방향 (0=타겟 방향, 1=위)")]
    [Range(0f, 1f)]
    public float knockbackUpRatio = 0.2f;
    
    [Tooltip("에어본 강도 (위로 띄우는 힘)")]
    [Range(0f, 50f)]
    public float launchForce = 15f;

    [Header("이동 설정")]
    [Tooltip("공격 중 이동 가능 여부")]
    public bool canMoveWhileAttacking = false;
    
    [Tooltip("공격 시 돌진 거리")]
    [Range(0f, 10f)]
    public float lungeDistance = 0f;

    [Header("VFX/SFX")]
    [Tooltip("공격 이펙트 프리팹")]
    public GameObject attackVFXPrefab;
    
    [Tooltip("공격 사운드")]
    public AudioClip attackSound;

    // 총 공격 사이클 시간
    public float TotalCycleDuration => windupDuration + attackDuration + recoveryDuration;
    
    // 최종 데미지 계산 (크리티컬 포함)
    public int CalculateDamage()
    {
        bool isCritical = Random.value < criticalChance; // 크리티컬 판정
        
        if (isCritical)
        {
            return Mathf.RoundToInt(baseDamage * criticalMultiplier); // 크리티컬 데미지
        }
        return baseDamage; // 기본 데미지
    }
    
    // 넉백 방향 계산
    public Vector3 CalculateKnockbackDirection(Vector3 attackDirection)
    {
        Vector3 upComponent = Vector3.up * knockbackUpRatio;                    // 위쪽 성분
        Vector3 horizontalComponent = attackDirection.normalized * (1f - knockbackUpRatio); // 수평 성분
        return (horizontalComponent + upComponent).normalized;                  // 정규화
    }
}
