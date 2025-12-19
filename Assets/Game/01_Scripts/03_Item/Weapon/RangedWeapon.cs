using System.Collections;
using System.Collections.Generic; // 필수
using UnityEngine;

public class RangedWeapon : Weapon
{
    [Header("Points")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Transform _ejectionPort;

    [Header("Visual & Audio")]
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _fireClip;
    [SerializeField] private AudioClip _reloadClip;
    [SerializeField] private AudioClip _emptyClip;

    // ★ 외부에서 MuzzlePoint에 접근하기 위한 프로퍼티
    public Transform myMuzzlePoint => _firePoint;

    private RangedWeaponData _gunData;
    private int _currentAmmo;
    private bool _isReloading = false;

    // ★ 아이템으로 얻은 추가 스탯들 (이 무기에만 적용됨)
    private int _bonusMaxAmmo = 0;
    private float _bonusSpreadReduction = 0f; // 탄퍼짐 감소량

    public bool HasAmmo => _currentAmmo > 0;
    public int CurrentAmmo => _currentAmmo;

    // 최대 탄창 = 기본 + 보너스
    public int MaxAmmo => (_gunData != null ? _gunData.maxAmmo : 0) + _bonusMaxAmmo;

    public bool IsReloading => _isReloading;

    private void OnEnable()
    {
        // 무기를 다시 꺼낼 때(SetActive true가 될 때) 실행됨

        _isReady = true;       // 쿨타임 강제 초기화 (이제 쏠 수 있음)
        _isReloading = false;  // 재장전 중이었다면 취소

        // 실행 중이던 모든 코루틴(타이머)을 끄고 새로 시작할 준비
        StopAllCoroutines();
    }

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint);
        _gunData = data as RangedWeaponData;

        // ★ 보너스 초기화는 '처음 생성될 때만' 해야 하는데, 
        // Initialize는 생성될 때 한 번 호출되므로 여기서 0으로 초기화해도 괜찮습니다.
        // (단, 데이터를 유지하고 싶다면 이 변수들을 초기화하는 코드를 빼야 합니다.)

        // 여기서는 "처음 주웠을 땐 0"이고, 업그레이드 후엔 유지되어야 하므로
        // 이 스크립트가 파괴되지 않는 한 변수는 유지됩니다.
        // 따라서 _bonusMaxAmmo = 0; 같은 코드는 넣지 않습니다. (PlayerWeaponController가 파괴를 안 하니까요!)

        if (_gunData != null && _currentAmmo == 0) // 처음 생성 때만 탄알 채우기
        {
            _currentAmmo = _gunData.maxAmmo;
        }

        if (ownerFirePoint != null) _firePoint = ownerFirePoint;
        else if (_firePoint == null) _firePoint = this.transform;

        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.0f;
        }
    }

    public override void Use()
    {
        if (!_isReady || _isReloading) return;

        if (_currentAmmo > 0) Fire();
        else
        {
            if (_audioSource != null && _emptyClip != null) _audioSource.PlayOneShot(_emptyClip);
            StartCoroutine(ReloadRoutine());
        }
    }

    public override void Reload()
    {
        if (!_isReloading && _currentAmmo < MaxAmmo)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    private void Fire()
    {
        _currentAmmo--;
        _isReady = false;

        if (_muzzleFlash != null) _muzzleFlash.Play();
        if (_audioSource != null && _fireClip != null)
        {
            _audioSource.pitch = Random.Range(0.95f, 1.05f);
            _audioSource.PlayOneShot(_fireClip);
        }

        if (_gunData.bulletPrefab != null && _firePoint != null)
        {
            int pellets = Mathf.Max(1, _gunData.pelletCount);

            for (int i = 0; i < pellets; i++)
            {
                // ★ 탄퍼짐 계산 (기본값 - 보너스 감소량)
                float currentSpread = Mathf.Max(0, _gunData.spreadAngle - _bonusSpreadReduction);

                float randomYaw = Random.Range(-currentSpread, currentSpread);
                float randomPitch = Random.Range(-currentSpread, currentSpread) * 0.2f;

                Quaternion spreadRotation = Quaternion.Euler(randomPitch, randomYaw, 0);
                Quaternion finalRotation = _firePoint.rotation * spreadRotation;

                GameObject bullet = Instantiate(_gunData.bulletPrefab, _firePoint.position, finalRotation);
                Projectile proj = bullet.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Setup(_gunData.damage, _gunData.bulletSpeed, _gunData.maxRange, _gunData.CalculatedKnockback);
                    proj.SetHitEffect(_gunData.hitEffectPrefab); // 히트 이펙트 전달
                }
            }
        }

        if (_gunData.casingPrefab != null && _ejectionPort != null)
        {
            GameObject casing = Instantiate(_gunData.casingPrefab, _ejectionPort.position, _ejectionPort.rotation);
            Rigidbody rb = casing.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 forceDirection = (_ejectionPort.right * Random.Range(2f, 3f)) + (_ejectionPort.up * Random.Range(0.5f, 1.0f));
                rb.AddForce(forceDirection.normalized * 3f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
            }
            Destroy(casing, 2.0f);
        }

        StartCoroutine(CoolTimeRoutine());
    }

    private IEnumerator CoolTimeRoutine()
    {
        yield return new WaitForSeconds(_baseData.coolTime);
        _isReady = true;
    }

    private IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        if (_audioSource != null && _reloadClip != null) _audioSource.PlayOneShot(_reloadClip);
        yield return new WaitForSeconds(_gunData.reloadTime);
        _currentAmmo = MaxAmmo;
        _isReloading = false;
        _isReady = true;
    }

    // --- 아이템 획득 함수들 ---

    public void UpgradeMagazine(int amount)
    {
        _bonusMaxAmmo += amount;
        _currentAmmo += amount; // 먹자마자 탄알도 채워줌
        Debug.Log($"탄창 확장! 현재 용량: {MaxAmmo}");
    }

    public void UpgradeGrip(float reductionAmount)
    {
        _bonusSpreadReduction += reductionAmount;
        Debug.Log($"수직 손잡이 장착! 탄퍼짐 감소량: {_bonusSpreadReduction}");
    }
}