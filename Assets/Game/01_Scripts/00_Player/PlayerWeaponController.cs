using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerWeaponController : MonoBehaviour
{
    [Header("Test")]
    public WeaponData testWeapon;

    [Header("Settings")]
    [SerializeField] private Transform _weaponHolder; // 무기 부착 위치 (오른손)

    [Header("Override")]
    [SerializeField] private Transform _playerFirePoint; // 발사/투척 원점

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Player _playerStats;
    [SerializeField] private PlayerController _playerController;

    [Header("State")]
    private Weapon _currentWeaponInstance;
    private bool _isSwapping = false;

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

        // 재장전 (원거리 무기만 해당)
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (_currentWeaponInstance.BaseData.weaponType == WeaponType.Ranged)
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
        if (data == null) return;

        // 2. 타입별 선행 조건 체크
        switch (data.weaponType)
        {
            case WeaponType.Melee:
                if (_playerStats != null && _playerStats.Stamina < 10) return;
                // _playerStats.UseStamina(10);
                break;

            case WeaponType.Ranged:
                var ranged = _currentWeaponInstance as RangedWeapon;
                if (ranged != null && !ranged.HasAmmo) return;
                break;

            case WeaponType.Throwable:
                // 추후 인벤토리에서 수류탄 개수 체크 로직 추가 필요
                break;
        }

        // 3. 애니메이션 실행
        if (_animator != null)
        {
            switch (data.weaponType)
            {
                case WeaponType.Melee:
                    _animator.SetTrigger("DoSwing");
                    break;
                case WeaponType.Ranged:
                    _animator.SetTrigger("DoShot");
                    break;
                case WeaponType.Throwable:
                    _animator.SetTrigger("DoSwing"); // 투척 애니메이션
                    break;
            }
        }

        // 4. 실제 무기 사용 (발사/휘두르기/던지기)
        _currentWeaponInstance.Use();
    }

    public void EquipWeapon(WeaponData newWeaponData)
    {
        if (_isSwapping || newWeaponData == null) return;
        StartCoroutine(SwapRoutine(newWeaponData));
    }

    public void UnequipWeapon()
    {
        if (_currentWeaponInstance != null)
        {
            Destroy(_currentWeaponInstance.gameObject);
            _currentWeaponInstance = null;
        }
    }

    private System.Collections.IEnumerator SwapRoutine(WeaponData newWeaponData)
    {
        _isSwapping = true;

        if (_animator != null) _animator.SetTrigger("DoSwap");

        if (_currentWeaponInstance != null)
        {
            Destroy(_currentWeaponInstance.gameObject);
        }

        yield return new WaitForSeconds(0.2f);

        if (newWeaponData.weaponPrefab != null)
        {
            GameObject weaponObj = Instantiate(newWeaponData.weaponPrefab, _weaponHolder);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = newWeaponData.weaponPrefab.transform.localRotation;

            _currentWeaponInstance = weaponObj.GetComponent<Weapon>();

            // ★ 중요: 무기 타입에 따라 컴포넌트가 제대로 붙어있는지 확인
            if (_currentWeaponInstance == null)
            {
                // 프리팹에 스크립트가 안 붙어있을 경우를 대비한 안전장치 (자동 부착)
                switch (newWeaponData.weaponType)
                {
                    case WeaponType.Melee:
                        _currentWeaponInstance = weaponObj.AddComponent<MeleeWeapon>();
                        break;
                    case WeaponType.Ranged:
                        _currentWeaponInstance = weaponObj.AddComponent<RangedWeapon>();
                        break;
                    case WeaponType.Throwable:
                        _currentWeaponInstance = weaponObj.AddComponent<ThrowableWeapon>();
                        break;
                }
            }

            if (_currentWeaponInstance != null)
            {
                _currentWeaponInstance.Initialize(newWeaponData, _playerFirePoint);
            }
        }

        yield return new WaitForSeconds(0.1f);
        _isSwapping = false;
    }
}