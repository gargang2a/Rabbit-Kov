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

    public Transform myMuzzlePoint => _firePoint;

    private RangedWeaponData _gunData;
    private int _currentAmmo;
    // private bool _isReloading = false; // ★ [삭제] 부모 변수 사용

    private int _bonusMaxAmmo = 0;
    private float _bonusSpreadReduction = 0f;

    private WaitForSeconds _waitCoolTime;
    private WaitForSeconds _waitReloadTime;

    public bool HasAmmo => _currentAmmo > 0;
    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => (_gunData != null ? _gunData.maxAmmo : 0) + _bonusMaxAmmo;

    // public bool IsReloading => _isReloading; // ★ [삭제] 부모에 이미 프로퍼티 있음

    private void OnEnable()
    {
        // 활성화 될 때 상태 초기화 (Weapon.OnDisable과 짝을 이룸)
        _isReady = true;
        _isReloading = false;
        StopAllCoroutines();

        if (_audioSource != null) _audioSource.pitch = 1.0f;
    }

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint);
        _gunData = data as RangedWeaponData;

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

        if (_currentAmmo <= 0) StartCoroutine(ReloadRoutine());
        else StartCoroutine(CoolTimeRoutine());
    }

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
        _isReloading = true; // 부모 변수
        _isReady = false;

        PlaySoundWithRandomPitch(_reloadClip, 0.95f, 1.05f);

        if (_waitReloadTime != null) yield return _waitReloadTime;
        else yield return new WaitForSeconds(_gunData.reloadTime);

        _currentAmmo = MaxAmmo;
        _isReloading = false; // 부모 변수
        _isReady = true;
    }

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