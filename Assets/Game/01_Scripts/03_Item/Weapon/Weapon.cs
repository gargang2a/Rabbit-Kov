using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{



    public enum WeaponType { Melee, Range }

    [Header("Weapon Settings")]
    [SerializeField] private WeaponType _type;
    [SerializeField] private int _damage = 20;
    [SerializeField] private float _coolTime = 0.3f;
    [SerializeField] private float _fireForce = 20f;

    [Header("Ammo Settings")]
    [SerializeField] private int _curAmmo;
    [SerializeField] private int _maxAmmo;
    [SerializeField] private float _reloadTime = 1.5f;

    [Header("References")]
    [SerializeField] private BoxCollider _meleeCollider;

    [Header("Gun & Bullet")]
    [SerializeField] private Transform _firePointPos;
    [SerializeField] private Transform _bulletCasePos;
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private GameObject _bulletCasePrefab;

    // Internal State
    private bool _isAttacking = false;
    private bool _isReloading = false;

    // Properties
    public WeaponType Type => _type;
    public float CoolTime => _coolTime;
    public bool IsAttacking => _isAttacking;
    public int CurAmmo => _curAmmo;
    public int MaxAmmo => _maxAmmo;
    public bool IsReloading => _isReloading;

    private void Awake()
    {
        if (_type == WeaponType.Melee && _meleeCollider == null)
            _meleeCollider = GetComponent<BoxCollider>();

        if (_meleeCollider != null)
            _meleeCollider.enabled = false;
    }

    // ★ [핵심 추가] 무기가 활성화될 때(꺼낼 때) 상태 초기화
    private void OnEnable()
    {
        // 이전 상태가 남아있어서 무기가 먹통이 되는 것을 방지
        _isAttacking = false;
        _isReloading = false;

        // 근접 무기 콜라이더도 켜져 있으면 끔
        if (_meleeCollider != null)
            _meleeCollider.enabled = false;
    }

    // 근접 공격 충돌 감지
    private void OnTriggerEnter(Collider other)
    {
        if (_type != WeaponType.Melee || !_isAttacking) return;
        if (other.CompareTag("Player") || other.CompareTag("Weapon")) return;

        // IDamageable 인터페이스를 가진 대상에게 데미지 전달
        if (other.TryGetComponent(out IDamageable target))
        {
            Vector3 attackDir = (other.transform.position - transform.position).normalized;
            target.TakeDamage(_damage, other.transform.position, attackDir);
            Debug.Log($"Melee Hit: {other.name}");
        }
    }

    // 공격 시도
    public void Use()
    {
        if (_isAttacking || _isReloading) return;

        if (_type == WeaponType.Melee)
        {
            StartCoroutine(SwingRoutine());
        }
        else if (_type == WeaponType.Range)
        {
            if (_curAmmo > 0)
            {
                _curAmmo--;
                StartCoroutine(ShotRoutine());
            }
            else
            {
                Debug.Log("탄약 부족! 재장전 필요.");
            }
        }
    }

    // 재장전 시도
    public void Reload()
    {
        if (_type == WeaponType.Melee || _isReloading || _curAmmo >= _maxAmmo) return;

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        Debug.Log("재장전 중...");

        yield return new WaitForSeconds(_reloadTime);

        _curAmmo = _maxAmmo;
        _isReloading = false;
        Debug.Log("재장전 완료");
    }

    private IEnumerator SwingRoutine()
    {
        _isAttacking = true;

        if (_meleeCollider != null) _meleeCollider.enabled = true;

        yield return new WaitForSeconds(_coolTime);

        if (_meleeCollider != null) _meleeCollider.enabled = false;

        _isAttacking = false;
    }

    private IEnumerator ShotRoutine()
    {
        _isAttacking = true;

        // 1. 총알 발사
        if (_bulletPrefab != null && _firePointPos != null)
        {
            GameObject instantBullet = Instantiate(_bulletPrefab, _firePointPos.position, _firePointPos.rotation);
            Rigidbody bulletRigid = instantBullet.GetComponent<Rigidbody>();

            if (bulletRigid != null)
                bulletRigid.velocity = _firePointPos.forward * _fireForce;
        }

        yield return null;

        // 2. 탄피 배출
        if (_bulletCasePrefab != null && _bulletCasePos != null)
        {
            GameObject instantCase = Instantiate(_bulletCasePrefab, _bulletCasePos.position, _bulletCasePos.rotation);
            Rigidbody caseRigid = instantCase.GetComponent<Rigidbody>();

            if (caseRigid != null)
            {
                Vector3 caseVec = (_bulletCasePos.right * Random.Range(2f, 3f)) + (Vector3.up * Random.Range(2f, 3f));
                caseRigid.AddForce(caseVec, ForceMode.Impulse);
                caseRigid.AddTorque(Vector3.up * 10, ForceMode.Impulse);
            }
        }

        yield return new WaitForSeconds(_coolTime);
        _isAttacking = false;
    }
}