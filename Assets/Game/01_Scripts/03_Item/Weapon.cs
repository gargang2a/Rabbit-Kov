using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public enum WeaponType { Melee, Range }

    [Header("Weapon Settings")]
    [SerializeField] private WeaponType _type;
    [SerializeField] private int _damage;
    [SerializeField] private float _coolTime = 0.3f;
    [SerializeField] private float _fireForce = 100f;

    [Header("Ammo Settings")]
    [SerializeField] private int _curAmmo;
    [SerializeField] private int _maxAmmo;

    [Header("References")]
    [SerializeField] private BoxCollider _meleeCollider;

    [Header("Gun & Bullet")]
    [SerializeField] private Transform _firePointPos;
    [SerializeField] private Transform _bulletCasePos;
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private GameObject _bulletCasePrefab;
    [SerializeField] private bool isReloading = false;

    // Internal State
    private bool _isAttacking = false;

    // Properties
    public WeaponType Type => _type;
    public float CoolTime => _coolTime;
    public bool IsAttacking => _isAttacking;
    public int CurAmmo => _curAmmo;
    public int MaxAmmo => _maxAmmo;

    private void Awake()
    {
        if (_meleeCollider == null)
            _meleeCollider = GetComponent<BoxCollider>();

        if (_meleeCollider != null)
            _meleeCollider.enabled = false;
    }

    public void Use()
    {
        if (_isAttacking) return;

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
                Debug.Log("탄약 부족!"); // 추후 UI 알림 연결
            }
        }
    }

    public void Reload()
    {
        if (_type == WeaponType.Melee) return;

        isReloading = true;
        Invoke("ReloadOut", 0.5f);
        
    }

    void RealodOut()
    {
        isReloading = false;
        _curAmmo = _maxAmmo;
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

        // [Optimization Warning] Instantiate는 비용이 큽니다. 추후 Object Pooling으로 교체 권장.
        if (_bulletPrefab != null && _firePointPos != null)
        {
            GameObject instantBullet = Instantiate(_bulletPrefab, _firePointPos.position, _firePointPos.rotation);
            Rigidbody bulletRigid = instantBullet.GetComponent<Rigidbody>();
            if (bulletRigid != null)
                bulletRigid.velocity = _firePointPos.forward * _fireForce; // 속도 변수화 권장
        }

        yield return null; // 한 프레임 대기 (탄피 배출 타이밍 조절)

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