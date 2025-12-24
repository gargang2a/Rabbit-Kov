using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// [역할] 1페이즈 공격 - 에어본 (플레이어를 공중으로 띄움)
public class AirborneAttack : MonoBehaviour, IBossAttack
{
    [Header("공격 설정")]
    [SerializeField] private string _attackName = "에어본 공격";
    [SerializeField] private float _cooldown = 3f;
    
    [Header("에어본 설정")]
    [Tooltip("에어본 범위 (반경)")]
    [SerializeField] private float _range = 5f;
    
    [Tooltip("위로 띄우는 힘")]
    [SerializeField] private float _launchForce = 15f;
    
    [Tooltip("데미지")]
    [SerializeField] private int _damage = 50;
    
    [Tooltip("선딜레이 (초)")]
    [SerializeField] private float _windupTime = 0.5f;
    
    [Header("이펙트")]
    [SerializeField] private GameObject _warningEffect;  // 범위 표시
    [SerializeField] private GameObject _impactEffect;   // 충격파
    
    private bool _isExecuting = false;      // 실행 중 여부
    private Coroutine _attackCoroutine;     // 공격 코루틴
    
    public string AttackName => _attackName;
    public float Cooldown => _cooldown;
    public bool IsExecuting => _isExecuting;
    
    // 데이터 초기화 (EnemyAttackDataSO에서 값 가져오기)
    public void Initialize(EnemyAttackDataSO data)
    {
        if (data == null)
        {
            Debug.LogWarning("[AirborneAttack] Initialize 실패: data가 null!");
            return;
        }
        
        _attackName = data.attackName;
        _cooldown = data.cooldown;
        _damage = data.baseDamage;
        _launchForce = data.launchForce;
        _range = data.attackRange;
        _windupTime = data.windupDuration;
        
        Debug.Log($"[AirborneAttack] Initialize 완료! LaunchForce: {_launchForce}, Range: {_range}, Damage: {_damage}");
    }

    // 공격 실행
    public void Execute(BossController boss, Transform target)
    {
        if (_isExecuting) return;
        
        _attackCoroutine = StartCoroutine(ExecuteAttackRoutine(boss, target));
    }

    // 공격 취소
    public void Cancel()
    {
        // 파괴된 객체 접근 방지
        if (this == null) return;
        
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }
        _isExecuting = false;
    }

    private IEnumerator ExecuteAttackRoutine(BossController boss, Transform target)
    {
        _isExecuting = true;
        Vector3 attackPos = target.position; // 플레이어 위치에서 공격

        // 1. 선딜레이 (경고 표시)
        if (_warningEffect != null)
        {
            GameObject warning = Instantiate(_warningEffect, attackPos, Quaternion.identity);
            warning.transform.localScale = Vector3.one * _range * 2f;
            Destroy(warning, _windupTime);
        }
        
        yield return new WaitForSeconds(_windupTime);

        // 2. 범위 내 플레이어 에어본
        Debug.Log($"[AirborneAttack] 범위 체크 시작! 위치: {attackPos}, 범위: {_range}");
        Collider[] hits = Physics.OverlapSphere(attackPos, _range);
        Debug.Log($"[AirborneAttack] 감지된 콜라이더 수: {hits.Length}");
        HashSet<Transform> hitTargets = new HashSet<Transform>(); // 중복 방지
        
        foreach (Collider hit in hits)
        {
            Debug.Log($"[AirborneAttack] 감지: {hit.name} (태그: {hit.tag})");
            
            if (hit.CompareTag("Player"))
            {
                // 이미 타격한 대상이면 스킵
                Transform rootTarget = hit.transform.root;
                if (hitTargets.Contains(rootTarget)) continue;
                hitTargets.Add(rootTarget);
                
                Debug.Log($"[AirborneAttack] 플레이어 발견! LaunchForce: {_launchForce}");
                
                // 에어본 (위로 발사)
                // 1. CharacterController 사용 시
                PlayerController pc = hit.GetComponent<PlayerController>();
                if (pc == null) pc = hit.GetComponentInParent<PlayerController>();
                if (pc != null)
                {
                    Debug.Log($"[AirborneAttack] PlayerController 발견! ApplyLaunch({_launchForce}) 호출!");
                    pc.ApplyLaunch(_launchForce);
                }
                else
                {
                    Debug.LogWarning($"[AirborneAttack] PlayerController를 찾을 수 없음!");
                    // 2. Rigidbody 사용 시 (Kinematic이 아닐 때)
                    Rigidbody rb = hit.GetComponent<Rigidbody>();
                    if (rb == null) rb = hit.GetComponentInParent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                        rb.AddForce(Vector3.up * _launchForce, ForceMode.Impulse);
                    }
                }
                
                // 데미지 적용
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    Vector3 attackDir = (hit.transform.position - attackPos).normalized;
                    damageable.TakeDamage(_damage, hit.transform.position, attackDir, _launchForce * 0.5f);
                    Debug.Log($"[AirborneAttack] 플레이어에게 {_damage} 데미지!");
                }
            }
        }

        // 3. 충격 이펙트 (범위 기반 스케일)
        if (_impactEffect != null)
        {
            GameObject impact = Instantiate(_impactEffect, attackPos, Quaternion.identity);
            impact.transform.localScale = Vector3.one * _range * 2f; // 범위 기반 스케일
            Destroy(impact, 2f);
        }
        
        // 4. 런타임 공격 범위 시각화 (Debug용)
        DrawAttackRange(attackPos, _range, 2f);

        _isExecuting = false;
    }
    
    // 런타임 공격 범위 시각화
    private void DrawAttackRange(Vector3 center, float radius, float duration)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0.1f, Mathf.Sin(angle1) * radius);
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0.1f, Mathf.Sin(angle2) * radius);
            
            Debug.DrawLine(p1, p2, Color.red, duration);
        }
        
        // 중심 표시
        Debug.DrawLine(center + Vector3.up * 0.1f, center + Vector3.up * 3f, Color.yellow, duration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _range);
    }
}
