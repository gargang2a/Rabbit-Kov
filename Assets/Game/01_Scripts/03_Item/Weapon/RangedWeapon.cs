using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon : Weapon
{
    [Header("Points (빈 GameObject를 만들어 위치를 잡으세요)")]
    // [할당 방법] 무기 모델의 '총구 끝' 위치에 빈 GameObject를 자식으로 만들고, 그 Transform을 드래그하세요.
    // [주의] 파란색 화살표(Z축)가 총알이 나갈 방향을 향하고 있어야 합니다.
    [SerializeField] private Transform _firePoint;      

    // [할당 방법] 무기 모델의 '탄피 배출구' 위치에 빈 GameObject를 자식으로 만들고, 그 Transform을 드래그하세요.
    // [역할] 여기서 탄피 프리팹이 생성되어 튀어 나갑니다.
    [SerializeField] private Transform _ejectionPort;   

    [Header("Visual & Audio (이펙트 및 사운드 파일)")]
    // [할당 방법] 총구 위치에 자식으로 넣어둔 'MuzzleFlash' 프리팹의 ParticleSystem 컴포넌트를 드래그하세요.
    // [설정] ParticleSystem의 'Play On Awake'는 꺼져 있어야 합니다.
    [SerializeField] private ParticleSystem _muzzleFlash; 

    // [할당 방법] 이 스크립트가 붙어있는 오브젝트(자기 자신)의 AudioSource 컴포넌트를 드래그해서 넣으세요.
    // (만약 비워두면 코드의 Initialize에서 자동으로 찾아줍니다.)
    [SerializeField] private AudioSource _audioSource;    

    // [할당 방법] Project 창에 있는 '발사 소리' 오디오 파일(.mp3, .wav)을 드래그하세요.
    [SerializeField] private AudioClip _fireClip;         

    // [할당 방법] Project 창에 있는 '재장전 소리' 오디오 파일(.mp3, .wav)을 드래그하세요.
    [SerializeField] private AudioClip _reloadClip;       

    // [할당 방법] Project 창에 있는 '빈 탄창(찰칵)' 오디오 파일(.mp3, .wav)을 드래그하세요.
    [SerializeField] private AudioClip _emptyClip;        


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
        // [최적화] Pitch를 약간 랜덤하게 주어 기관총 소리가 기계적이지 않게 들리도록 함
        if (_audioSource != null && _fireClip != null)
        {
            _audioSource.pitch = Random.Range(0.95f, 1.05f);
            _audioSource.PlayOneShot(_fireClip);
        }
        // 2. 총알 생성
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
                    proj.Setup(_gunData.damage, _gunData.bulletSpeed, _gunData.maxRange, _gunData.CalculatedKnockback);
                    proj.SetHitEffect(_gunData.hitEffectPrefab); // 히트 이펙트 전달
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