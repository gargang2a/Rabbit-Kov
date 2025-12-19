using UnityEngine;
using UnityEngine.EventSystems; // UI 클릭 방지용
using System.Collections.Generic; // ★ Dictionary 사용을 위해 필수

public class PlayerWeaponController : MonoBehaviour
{
    [Header("Test")]
    public WeaponData testWeapon;

    [Header("Settings")]
    [SerializeField] private Transform _weaponHolder; // 무기가 생성될 부모(오른손)
    [SerializeField] private Transform _playerFirePoint; // 플레이어 기준 발사 위치

    [Header("Effects")]
    [SerializeField] private ParticleSystem _muzzleFlash; // 총구 화염 이펙트

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Player _playerStats;
    [SerializeField] private PlayerController _playerController;

    [Header("State")]
    private Weapon _currentWeaponInstance;
    private bool _isSwapping = false;

    // ★ [핵심] 생성된 무기들을 저장해두는 보관함 (캐싱)
    // 한 번 만든 무기는 파괴하지 않고 여기에 넣어뒀다가 다시 꺼내 씁니다.
    private Dictionary<WeaponData, Weapon> _weaponCache = new Dictionary<WeaponData, Weapon>();

    public Weapon CurrentWeapon => _currentWeaponInstance;

    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_playerStats == null) _playerStats = GetComponent<Player>();
        if (_playerController == null) _playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        if (testWeapon != null)
        {
            EquipWeapon(testWeapon);
        }
    }

    private void Update()
    {
        if (_currentWeaponInstance == null || _isSwapping) return;
        if (_playerController != null && _playerController.IsRolling) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // 공격 입력
        if (Input.GetButton("Fire1"))
        {
            TryAttack();
        }

        // 재장전 입력
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 원거리 무기일 때만 재장전
            if (_currentWeaponInstance is RangedWeapon)
            {
                _currentWeaponInstance.Reload();
                if (_animator != null) _animator.SetTrigger("DoReload");
            }
        }
    }

    private void TryAttack()
    {
        // 1. 무기 준비 상태 확인
        if (!_currentWeaponInstance.IsReady) return;

        WeaponData data = _currentWeaponInstance.BaseData;

        // 2. 타입별 체크 (스태미너 등)
        if (_currentWeaponInstance is MeleeWeapon)
        {
            if (_playerStats != null && _playerStats.Stamina < 10) return;
            // _playerStats.UseStamina(10); 
        }
        else if (_currentWeaponInstance is RangedWeapon ranged)
        {
            if (!ranged.HasAmmo) return;
        }

        // 3. 애니메이션 및 이펙트 실행
        if (_animator != null)
        {
            if (_currentWeaponInstance is RangedWeapon)
            {
                // 총구 화염 위치 동기화
                if (_muzzleFlash != null)
                {
                    // 무기에 전용 총구 위치(MuzzlePoint)가 있으면 거기로 이동
                    if (_currentWeaponInstance is RangedWeapon rWeapon && rWeapon.myMuzzlePoint != null)
                    {
                        _muzzleFlash.transform.position = rWeapon.myMuzzlePoint.position;
                        _muzzleFlash.transform.rotation = rWeapon.myMuzzlePoint.rotation;
                    }
                    else
                    {
                        _muzzleFlash.transform.position = _playerFirePoint.position;
                        _muzzleFlash.transform.rotation = _playerFirePoint.rotation;
                    }
                    _muzzleFlash.Play();
                }
                _animator.SetTrigger("DoShot");
            }
            else if (_currentWeaponInstance is MeleeWeapon)
            {
                _animator.SetTrigger("DoSwing");
            }
        }

        // 4. 실제 무기 사용
        _currentWeaponInstance.Use();
    }

    public void EquipWeapon(WeaponData newWeaponData)
    {
        if (_isSwapping || newWeaponData == null) return;

        // 이미 같은 무기를 들고 있다면 교체 안 함 (최적화)
        if (_currentWeaponInstance != null && _currentWeaponInstance.BaseData == newWeaponData) return;

        StartCoroutine(SwapRoutine(newWeaponData));
    }

    public void UnequipWeapon()
    {
        if (_currentWeaponInstance != null)
        {
            // ★ Destroy 대신 SetActive(false) 사용
            _currentWeaponInstance.gameObject.SetActive(false);
            _currentWeaponInstance = null;
        }
    }

    private System.Collections.IEnumerator SwapRoutine(WeaponData newWeaponData)
    {
        _isSwapping = true;

        if (_animator != null) _animator.SetTrigger("DoSwap");

        // 1. 기존 무기 숨기기 (끄기)
        if (_currentWeaponInstance != null)
        {
            _currentWeaponInstance.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(0.2f);

        // 2. 새 무기 꺼내기 (보관함 확인)
        if (_weaponCache.ContainsKey(newWeaponData) && _weaponCache[newWeaponData] != null)
        {
            // A. 이미 만들었던 무기가 있다! -> 켜기만 하면 됨 (업그레이드 정보 유지됨)
            _currentWeaponInstance = _weaponCache[newWeaponData];
            _currentWeaponInstance.gameObject.SetActive(true);
        }
        else
        {
            // B. 처음 드는 무기다! -> 새로 생성(Instantiate)
            if (newWeaponData.weaponPrefab != null)
            {
                GameObject weaponObj = Instantiate(newWeaponData.weaponPrefab, _weaponHolder);
                weaponObj.transform.localPosition = Vector3.zero;
                weaponObj.transform.localRotation = newWeaponData.weaponPrefab.transform.localRotation;

                Weapon newWeapon = weaponObj.GetComponent<Weapon>();

                // 스크립트 없으면 자동 부착 (안전장치)
                if (newWeapon == null)
                {
                    if (newWeaponData is RangedWeaponData)
                        newWeapon = weaponObj.AddComponent<RangedWeapon>();
                    // else if (newWeaponData is MeleeWeaponData) ... 추가 가능
                }

                if (newWeapon != null)
                {
                    // 최초 1회만 초기화 (탄알 채우기 등)
                    newWeapon.Initialize(newWeaponData, _playerFirePoint);

                    // ★ 보관함에 등록
                    _weaponCache.Add(newWeaponData, newWeapon);
                    _currentWeaponInstance = newWeapon;
                }
            }
        }

        yield return new WaitForSeconds(0.1f);
        _isSwapping = false;
    }
}