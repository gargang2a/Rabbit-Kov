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

        // 1. 시각/청각 효과
        if (_muzzleFlash != null) _muzzleFlash.Play();
        if (_audioSource != null && _fireClip != null) _audioSource.PlayOneShot(_fireClip);

        // 2. 총알 생성 (상하좌우 탄퍼짐 적용)
        if (_gunData.bulletPrefab != null && _firePoint != null)
        {
            int pellets = Mathf.Max(1, _gunData.pelletCount);

            for (int i = 0; i < pellets; i++)
            {
                // [수정됨] -------------------------------------------------------
                // A. 좌우(Yaw) 랜덤 각도 계산 (Y축 회전)
                float randomYaw = Random.Range(-_gunData.spreadAngle, _gunData.spreadAngle);

                // B. 상하(Pitch) 랜덤 각도 계산 (X축 회전) ★ 추가됨
                // (상하 퍼짐은 보통 좌우보다 조금 덜 퍼지게 하는 게 자연스러워서 0.5f를 곱하기도 함. 취향껏 조절)
                float randomPitch = Random.Range(-_gunData.spreadAngle, _gunData.spreadAngle) * 0.2f;

                // C. X축(상하), Y축(좌우) 모두 적용하여 회전값 생성
                Quaternion spreadRotation = Quaternion.Euler(randomPitch, randomYaw, 0);
                // ---------------------------------------------------------------

                // D. 최종 발사 각도 적용
                Quaternion finalRotation = _firePoint.rotation * spreadRotation;

                // E. 총알 생성
                GameObject bullet = Instantiate(_gunData.bulletPrefab, _firePoint.position, finalRotation);

                Projectile proj = bullet.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Setup(_gunData.damage, _gunData.bulletSpeed, _gunData.maxRange);
                }
            }
        }

        // 3. 탄피 배출 (기존 코드 동일)
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
        // Debug.Log("Reloading..."); // 로그는 필요 없으면 주석 처리

        // 재장전 소리 재생
        if (_audioSource != null && _reloadClip != null) _audioSource.PlayOneShot(_reloadClip);

        yield return new WaitForSeconds(_gunData.reloadTime);

        _currentAmmo = _gunData.maxAmmo;
        _isReloading = false;
        _isReady = true;
        // Debug.Log("Reload Complete!");
    }
}