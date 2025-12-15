using System.Collections;
using UnityEngine;

public class Jongjun_Weapon : MonoBehaviour
{
    public enum WeaponType { Melee, Range }

    [Header("Weapon Settings")]
    [SerializeField] private WeaponType _type;
    [SerializeField] private int _damage = 20; // 데미지 예시
    [SerializeField] private float _coolTime = 0.3f;
    [SerializeField] private float _fireForce = 20f; // 총알 발사 속도

    [Header("Ammo Settings")]
    [SerializeField] private int _curAmmo;
    [SerializeField] private int _maxAmmo;
    [SerializeField] private float _reloadTime = 1.5f; // 재장전 소요 시간

    [Header("References")]
    [SerializeField] private BoxCollider _meleeCollider; // 근접 무기용 콜라이더

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
        // 근접 무기일 경우 콜라이더 자동 할당 시도
        if (_type == WeaponType.Melee && _meleeCollider == null)
            _meleeCollider = GetComponent<BoxCollider>();

        // 시작 시 근접 콜라이더 비활성화 (공격할 때만 켬)
        if (_meleeCollider != null)
            _meleeCollider.enabled = false;
    }

    /// <summary>
    /// 🔴 [New] 근접 공격 충돌 감지 로직
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 근접 무기가 아니거나 공격 중이 아니면 무시
        if (_type != WeaponType.Melee || !_isAttacking) return;

        // 플레이어 자신이나 다른 무기와의 충돌 방지
        if (other.CompareTag("Player") || other.CompareTag("Weapon")) return;

        // IDamageable 인터페이스를 가진 대상(적, 오브젝트)인지 확인
        if (other.TryGetComponent(out IDamageable target))
        {
            // 공격 방향 계산 (플레이어 -> 적)
            // 근접 공격은 정확한 타격 지점을 알기 어려우므로 적의 중심점을 사용하거나 무기 위치 사용
            Vector3 attackDir = (other.transform.position - transform.position).normalized;

            // 데미지 전달
            target.TakeDamage(_damage, other.transform.position, attackDir);

            Debug.Log($"Melee Hit: {other.name}");
        }
    }

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
                // 여기에 '틱' 하는 빈 총 소리 재생 로직 추가 가능
            }
        }
    }

    public void Reload()
    {
        // 근접 무기거나, 이미 재장전 중이거나, 탄약이 꽉 찼으면 무시
        if (_type == WeaponType.Melee || _isReloading || _curAmmo >= _maxAmmo) return;

        StartCoroutine(ReloadRoutine());
    }

    /// <summary>
    /// 🟡 [Fix] Invoke 대신 Coroutine을 사용하여 로직 제어 강화
    /// </summary>
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

        // 공격 판정 활성화
        if (_meleeCollider != null) _meleeCollider.enabled = true;

        yield return new WaitForSeconds(_coolTime);

        // 공격 판정 비활성화
        if (_meleeCollider != null) _meleeCollider.enabled = false;

        _isAttacking = false;
    }

    private IEnumerator ShotRoutine()
    {
        _isAttacking = true;

        // 1. 총알 발사
        if (_bulletPrefab != null && _firePointPos != null)
        {
            // [Optimization Note] 빈번한 발사는 Object Pooling 사용 권장
            GameObject instantBullet = Instantiate(_bulletPrefab, _firePointPos.position, _firePointPos.rotation);
            Rigidbody bulletRigid = instantBullet.GetComponent<Rigidbody>();

            if (bulletRigid != null)
                bulletRigid.velocity = _firePointPos.forward * _fireForce;
        }

        yield return null; // 한 프레임 대기 (탄피 배출 타이밍)

        // 2. 탄피 배출
        if (_bulletCasePrefab != null && _bulletCasePos != null)
        {
            GameObject instantCase = Instantiate(_bulletCasePrefab, _bulletCasePos.position, _bulletCasePos.rotation);
            Rigidbody caseRigid = instantCase.GetComponent<Rigidbody>();

            if (caseRigid != null)
            {
                // 오른쪽 위 + 랜덤 방향으로 튀어나감
                Vector3 caseVec = (_bulletCasePos.right * Random.Range(2f, 3f)) + (Vector3.up * Random.Range(2f, 3f));
                caseRigid.AddForce(caseVec, ForceMode.Impulse);
                caseRigid.AddTorque(Vector3.up * 10, ForceMode.Impulse);
            }
        }

        yield return new WaitForSeconds(_coolTime);
        _isAttacking = false;
    }
}