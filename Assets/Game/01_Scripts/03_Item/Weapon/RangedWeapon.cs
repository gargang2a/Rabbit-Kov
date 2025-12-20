using System.Collections;
using System.Collections.Generic;
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

    // 외부 접근 프로퍼티
    public Transform myMuzzlePoint => _firePoint;

    private RangedWeaponData _gunData;
    private int _currentAmmo;
    private bool _isReloading = false;

    // 아이템 보너스 스탯
    private int _bonusMaxAmmo = 0;
    private float _bonusSpreadReduction = 0f;

    // [Optimization] GC 방지를 위한 코루틴 대기 시간 캐싱
    private WaitForSeconds _waitCoolTime;
    private WaitForSeconds _waitReloadTime;

    public bool HasAmmo => _currentAmmo > 0;
    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => (_gunData != null ? _gunData.maxAmmo : 0) + _bonusMaxAmmo;
    public bool IsReloading => _isReloading;

    private void OnEnable()
    {
        _isReady = true;
        _isReloading = false;
        StopAllCoroutines();

        // 비활성화 후 다시 켤 때 피치가 변경된 상태로 남지 않도록 초기화
        if (_audioSource != null) _audioSource.pitch = 1.0f;
    }

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint);
        _gunData = data as RangedWeaponData;

        if (_gunData != null)
        {
            if (_currentAmmo == 0) _currentAmmo = _gunData.maxAmmo;

            // [Optimization] 데이터 로드 시점에 WaitForSeconds 캐싱
            _waitCoolTime = new WaitForSeconds(_baseData.coolTime);
            _waitReloadTime = new WaitForSeconds(_gunData.reloadTime);
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

        if (_currentAmmo > 0)
        {
            Fire();
        }
        else
        {
            // 빈 탄창 소리 (랜덤 피치)
            PlaySoundWithRandomPitch(_emptyClip, 0.9f, 1.1f);
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

        // 사격 소리 (랜덤 피치)
        PlaySoundWithRandomPitch(_fireClip, 0.95f, 1.05f);

        if (_gunData.bulletPrefab != null && _firePoint != null)
        {
            int pellets = Mathf.Max(1, _gunData.pelletCount);

            for (int i = 0; i < pellets; i++)
            {
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
                    proj.SetHitEffect(_gunData.hitEffectPrefab);
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

        // ★ [Auto Reload Logic] 탄알이 0이면 즉시 재장전, 아니면 쿨타임 대기
        if (_currentAmmo <= 0)
        {
            StartCoroutine(ReloadRoutine());
        }
        else
        {
            StartCoroutine(CoolTimeRoutine());
        }
    }

    // 사운드 피치 랜덤 재생 헬퍼 함수
    private void PlaySoundWithRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (_audioSource == null || clip == null) return;

        _audioSource.pitch = Random.Range(minPitch, maxPitch);
        _audioSource.PlayOneShot(clip);
    }

    private IEnumerator CoolTimeRoutine()
    {
        if (_waitCoolTime != null) yield return _waitCoolTime;
        else yield return new WaitForSeconds(_baseData.coolTime);

        _isReady = true;
    }

    private IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        _isReady = false; // 재장전 중 발사 방지

        // 재장전 소리 (랜덤 피치)
        PlaySoundWithRandomPitch(_reloadClip, 0.95f, 1.05f);

        if (_waitReloadTime != null) yield return _waitReloadTime;
        else yield return new WaitForSeconds(_gunData.reloadTime);

        _currentAmmo = MaxAmmo;
        _isReloading = false;
        _isReady = true;
    }

    // --- 아이템 획득 함수들 ---

    public void UpgradeMagazine(int amount)
    {
        _bonusMaxAmmo += amount;
        _currentAmmo += amount;
        Debug.Log($"탄창 확장! 현재 용량: {MaxAmmo}");
    }

    public void UpgradeGrip(float reductionAmount)
    {
        _bonusSpreadReduction += reductionAmount;
        Debug.Log($"수직 손잡이 장착! 탄퍼짐 감소량: {_bonusSpreadReduction}");
    }
}