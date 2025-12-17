using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon : Weapon
{
    [Header("Points")]
    [SerializeField] private Transform _firePoint;      // 총구 위치
    [SerializeField] private Transform _ejectionPort;   // 탄피 배출구

    [Header("Visual & Audio")]
    [SerializeField] private ParticleSystem _muzzleFlash; // 총구 화염 이펙트
    [SerializeField] private AudioSource _audioSource;    // 소리 재생기
    [SerializeField] private AudioClip _fireClip;         // 발사 소리
    [SerializeField] private AudioClip _reloadClip;       // 재장전 소리
    [SerializeField] private AudioClip _emptyClip;        // 빈 탄창 소리 (찰칵)

    private RangedWeaponData _gunData;
    private int _currentAmmo;
    private bool _isReloading = false;

    // ★ [필수] 컨트롤러에서 탄약 상태를 체크하기 위한 프로퍼티
    public bool HasAmmo => _currentAmmo > 0;
    public int CurrentAmmo => _currentAmmo; // UI 표시용

    public int MaxAmmo => _gunData != null ? _gunData.maxAmmo : 0;
    public bool IsReloading => _isReloading;

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint); // 부모 호출
        _gunData = data as RangedWeaponData;

        if (_gunData != null)
        {
            _currentAmmo = _gunData.maxAmmo;
        }

        // ★ 만약 외부(플레이어)에서 발사 위치를 지정해줬다면 그것을 사용
        if (ownerFirePoint != null)
        {
            _firePoint = ownerFirePoint;
        }
        // 지정 안 해줬는데 프리팹에도 연결 안 되어 있다면? -> 내 위치 사용 (에러 방지)
        else if (_firePoint == null)
        {
            _firePoint = this.transform;
        }


        // 오디오 소스 컴포넌트가 없으면 자동 추가 (안전장치)
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.0f; // 3D 사운드
        }
    }

    public override void Use()
    {
        // 쿨타임 중이거나 재장전 중이면 무시
        if (!_isReady || _isReloading) return;

        if (_currentAmmo > 0)
        {
            Fire();
        }
        else
        {
            // 탄약 없음: 빈 탄창 소리 재생
            if (_audioSource != null && _emptyClip != null)
            {
                _audioSource.PlayOneShot(_emptyClip);
            }

            // 자동 재장전 시도
            StartCoroutine(ReloadRoutine());
        }
    }

    public override void Reload()
    {
        // 이미 재장전 중이거나 탄약이 꽉 찼으면 무시
        if (!_isReloading && _currentAmmo < _gunData.maxAmmo)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    private void Fire()
    {
        _currentAmmo--;
        _isReady = false; // 쿨타임 시작

        // 1. 시각/청각 효과 (Juice)
        if (_muzzleFlash != null) _muzzleFlash.Play();
        if (_audioSource != null && _fireClip != null) _audioSource.PlayOneShot(_fireClip);

        // 2. 총알 생성
        if (_gunData.bulletPrefab != null && _firePoint != null)
        {
            GameObject bullet = Instantiate(_gunData.bulletPrefab, _firePoint.position, _firePoint.rotation);
            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Setup(_gunData.damage, _gunData.bulletSpeed, _gunData.maxRange);
            }
        }

        // 3. 탄피 배출 및 물리 효과
        if (_gunData.casingPrefab != null && _ejectionPort != null)
        {
            GameObject casing = Instantiate(_gunData.casingPrefab, _ejectionPort.position, _ejectionPort.rotation);

            // ★ 탄피가 튀어 나가는 물리 힘 적용
            Rigidbody rb = casing.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 오른쪽 + 위쪽 방향으로 힘을 가함 (Random을 섞어 자연스럽게)
                Vector3 forceDirection = (_ejectionPort.right * Random.Range(2f, 3f)) + (_ejectionPort.up * Random.Range(0.5f, 1.0f));
                rb.AddForce(forceDirection.normalized * 3f, ForceMode.Impulse); // 3f는 튀는 강도

                // 회전력 추가 (빙글빙글 돌면서 떨어지게)
                rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
            }

            Destroy(casing, 2.0f); // 2초 뒤 삭제
        }

        // 4. 연사 속도 대기
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
        Debug.Log("Reloading...");

        // 재장전 소리 재생
        if (_audioSource != null && _reloadClip != null) _audioSource.PlayOneShot(_reloadClip);

        yield return new WaitForSeconds(_gunData.reloadTime);

        _currentAmmo = _gunData.maxAmmo;
        _isReloading = false;
        _isReady = true;
        Debug.Log("Reload Complete!");
    }
}