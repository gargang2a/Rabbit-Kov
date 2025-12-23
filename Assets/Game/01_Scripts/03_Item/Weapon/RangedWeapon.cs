using System.Collections;
using UnityEngine;
using System;

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

    public event Action<int, int> OnAmmoChanged;

    public Transform myMuzzlePoint => _firePoint;

    private RangedWeaponData _gunData;
    private int _currentAmmo;

    private PlayerAmmoManager _ammoManager;
    private Player _ownerPlayer;

    private int _bonusMaxAmmo = 0;
    private float _bonusSpreadReduction = 0f;

    private WaitForSeconds _waitCoolTime;
    private WaitForSeconds _waitReloadTime;

    public bool HasAmmo => _currentAmmo > 0;
    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => (_gunData != null ? _gunData.maxAmmo : 0) + _bonusMaxAmmo;

    // ★ [핵심 추가] 외부(UI)에서 최종 반동 값을 알 수 있게 하는 프로퍼티
    public float FinalSpread
    {
        get
        {
            if (_gunData == null) return 0f;

            // 플레이어 스탯 (없으면 0)
            float playerReduction = (_ownerPlayer != null) ? _ownerPlayer.SpreadReduction : 0f;

            // 기본값 - (무기 부착물 보너스 + 플레이어 스탯 보너스)
            float spread = _gunData.spreadAngle - (_bonusSpreadReduction + playerReduction);

            // 0보다 작아지지 않게 보정
            return Mathf.Max(0, spread);
        }
    }

    private void OnEnable()
    {
        _isReady = true;
        _isReloading = false;
        StopAllCoroutines();

        if (_audioSource != null) _audioSource.pitch = 1.0f;

        UpdateAmmoUI();
    }

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint);
        _gunData = data as RangedWeaponData;

        _ammoManager = GetComponentInParent<PlayerAmmoManager>();
        _ownerPlayer = GetComponentInParent<Player>();

        if (_gunData != null)
        {
            if (_currentAmmo == 0) _currentAmmo = _gunData.maxAmmo;
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

        UpdateAmmoUI();
    }

    public void UpdateAmmoUI()
    {
        if (OnAmmoChanged != null)
        {
            int reserveAmmo = 0;
            if (_ammoManager != null && _gunData != null && _gunData.ammoItemData != null)
            {
                reserveAmmo = _ammoManager.GetAmmoCount(_gunData.ammoItemData);
            }
            OnAmmoChanged.Invoke(_currentAmmo, reserveAmmo);
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
        PlaySoundWithRandomPitch(_fireClip, 0.95f, 1.05f);

        // ★ [수정] 위에서 만든 FinalSpread 프로퍼티를 사용하여 탄퍼짐 적용
        float currentSpread = FinalSpread;

        if (_gunData.bulletPrefab != null && _firePoint != null)
        {
            int pellets = Mathf.Max(1, _gunData.pelletCount);
            for (int i = 0; i < pellets; i++)
            {
                float randomYaw = UnityEngine.Random.Range(-currentSpread, currentSpread);
                float randomPitch = UnityEngine.Random.Range(-currentSpread, currentSpread) * 0.2f;

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
                Vector3 forceDirection = (_ejectionPort.right * UnityEngine.Random.Range(2f, 3f)) + (_ejectionPort.up * UnityEngine.Random.Range(0.5f, 1.0f));
                rb.AddForce(forceDirection.normalized * 3f, ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 10f, ForceMode.Impulse);
            }
            Destroy(casing, 2.0f);
        }

        UpdateAmmoUI();

        if (_currentAmmo <= 0) StartCoroutine(ReloadRoutine());
        else StartCoroutine(CoolTimeRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        if (_ammoManager == null || _gunData == null || _gunData.ammoItemData == null)
        {
            _isReloading = false;
            _isReady = true;
            yield break;
        }

        int ammoInWallet = _ammoManager.GetAmmoCount(_gunData.ammoItemData);
        if (ammoInWallet <= 0)
        {
            _isReloading = false;
            _isReady = true;
            yield break;
        }

        _isReloading = true;
        _isReady = false;

        PlaySoundWithRandomPitch(_reloadClip, 0.95f, 1.05f);

        if (_waitReloadTime != null) yield return _waitReloadTime;
        else yield return new WaitForSeconds(_gunData.reloadTime);

        int needed = MaxAmmo - _currentAmmo;
        int toLoad = Mathf.Min(needed, ammoInWallet);

        if (_ammoManager.ConsumeAmmo(_gunData.ammoItemData, toLoad))
        {
            _currentAmmo += toLoad;
        }

        _isReloading = false;
        _isReady = true;

        UpdateAmmoUI();
    }

    private void PlaySoundWithRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (_audioSource == null || clip == null) return;
        _audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        _audioSource.PlayOneShot(clip);
    }

    private IEnumerator CoolTimeRoutine()
    {
        if (_waitCoolTime != null) yield return _waitCoolTime;
        else yield return new WaitForSeconds(_baseData.coolTime);
        _isReady = true;
    }

    public void UpgradeMagazine(int amount)
    {
        _bonusMaxAmmo += amount;
        _currentAmmo += amount;
        UpdateAmmoUI();
    }

    public void UpgradeGrip(float reductionAmount)
    {
        _bonusSpreadReduction += reductionAmount;
        // UI 자동 갱신은 Update에서 polling하므로 별도 호출 불필요
    }
}