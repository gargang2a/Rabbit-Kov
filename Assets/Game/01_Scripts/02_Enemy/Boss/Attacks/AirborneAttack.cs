using UnityEngine;
using System.Collections;

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

    // 공격 실행
    public void Execute(BossController boss, Transform target)
    {
        if (_isExecuting) return;
        
        _attackCoroutine = StartCoroutine(ExecuteAttackRoutine(boss, target));
    }

    // 공격 취소
    public void Cancel()
    {
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
        Vector3 attackPos = boss.transform.position;

        // 1. 선딜레이 (경고 표시)
        if (_warningEffect != null)
        {
            GameObject warning = Instantiate(_warningEffect, attackPos, Quaternion.identity);
            warning.transform.localScale = Vector3.one * _range * 2f;
            Destroy(warning, _windupTime);
        }
        
        yield return new WaitForSeconds(_windupTime);

        // 2. 범위 내 플레이어 에어본
        Collider[] hits = Physics.OverlapSphere(attackPos, _range);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // 에어본 (위로 발사)
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // 수직 속도 초기화
                    rb.AddForce(Vector3.up * _launchForce, ForceMode.Impulse);
                    Debug.Log($"[AirborneAttack] 플레이어 에어본! (Force: {_launchForce})");
                }
                
                // TODO: CharacterController 에어본 처리
            }
        }

        // 3. 충격 이펙트
        if (_impactEffect != null)
        {
            GameObject impact = Instantiate(_impactEffect, attackPos, Quaternion.identity);
            Destroy(impact, 2f);
        }

        _isExecuting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _range);
    }
}
