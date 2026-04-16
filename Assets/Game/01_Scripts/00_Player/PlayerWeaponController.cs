using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PlayerWeaponController : MonoBehaviour
{
    [Header("Test")]
    public WeaponData testWeapon;

    [Header("Settings")]
    [SerializeField] private Transform _weaponHolder;
    [SerializeField] private Transform _playerFirePoint;

    [Header("Effects")]
    [SerializeField] private ParticleSystem _muzzleFlash;

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Player _playerStats;
    [SerializeField] private PlayerController _playerController;

    // ★ [추가] 인벤토리 참조 (아이템 소모를 위해 필요)
    [SerializeField] private Inventory _inventory;

    [Header("State")]
    private Weapon _currentWeaponInstance;
    private bool _isSwapping = false;

    private Dictionary<WeaponData, Weapon> _weaponCache = new Dictionary<WeaponData, Weapon>();

    public Weapon CurrentWeapon => _currentWeaponInstance;

    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_playerStats == null) _playerStats = GetComponent<Player>();
        if (_playerController == null) _playerController = GetComponent<PlayerController>();

        // ★ [추가] 인벤토리 컴포넌트 가져오기
        if (_inventory == null) _inventory = GetComponent<Inventory>();
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

        if (Input.GetButton("Fire1"))
        {
            TryAttack();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (_currentWeaponInstance is RangedWeapon)
            {
                _currentWeaponInstance.Reload();
                if (_animator != null) _animator.SetTrigger("DoReload");
            }
        }
    }

    private void TryAttack()
    {
        if (!_currentWeaponInstance.IsReady) return;

        WeaponData data = _currentWeaponInstance.BaseData;

        // 1. 스태미너 및 탄약 체크
        if (_currentWeaponInstance is MeleeWeapon)
        {
            if (_playerStats != null && _playerStats.Stamina < 10) return;
            // _playerStats.UseStamina(10); 
        }
        else if (_currentWeaponInstance is RangedWeapon ranged)
        {
            if (!ranged.HasAmmo) return;
        }

        // 2. 애니메이션 및 이펙트
        if (_animator != null)
        {
            if (_currentWeaponInstance is RangedWeapon)
            {
                if (_muzzleFlash != null)
                {
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
            // 투척 무기는 보통 별도의 Trigger나 Animation이 있을 수 있음 (여기서는 생략)
        }

        // 3. 실제 무기 사용 (발사체 생성 등)
        _currentWeaponInstance.Use();

        // ★★★ [핵심 수정] 투척 무기일 경우 아이템 소모 및 처리 로직 실행
        if (_currentWeaponInstance is ThrowableWeapon)
        {
            HandleThrowableConsumption(data);
        }
    }

    // ★ [추가] 투척 무기 소모 처리 메서드
    private void HandleThrowableConsumption(WeaponData data)
    {
        // 1. 인벤토리에서 아이템 1개 제거
        if (_inventory != null)
        {
            _inventory.RemoveItem(data);
        }

        // 2. 인벤토리에 해당 아이템이 더 남아있는지 확인
        // (Inventory.Items 리스트에 같은 데이터가 있는지 확인)
        bool hasMore = _inventory.Items.Contains(data);

        if (!hasMore)
        {
            // 더 이상 남은 수류탄이 없으면 장착 해제 및 파괴
            UnequipAndDestroyCurrent();
        }
        else
        {
            // 남은 수류탄이 있다면? 
            // 기획 의도에 따라:
            // A. 계속 들고 있게 한다 (연속 투척 가능) -> 아무것도 안 함
            // B. 일단 손에서 없애고 다시 꺼내게 한다 -> 코루틴으로 재장착 처리

            // 여기서는 "한번만 던질 수 있게"라는 요청에 맞춰, 
            // 현재 들고 있는 인스턴스를 제거하여 시각적으로 손을 비웁니다.
            // (플레이어가 다시 키를 눌러 장착하거나, 자동 재장착 로직을 추가해야 함)

            // 만약 연속 투척을 막고 싶다면 아래 주석을 해제하여 강제 해제하세요.
            UnequipAndDestroyCurrent();
        }
    }

    // ★ [추가] 현재 무기를 완전히 제거하는 헬퍼 함수
    private void UnequipAndDestroyCurrent()
    {
        if (_currentWeaponInstance == null) return;

        WeaponData data = _currentWeaponInstance.BaseData;

        // 캐시에서 제거 (다음에 다시 장착할 때 새로 생성하기 위함, 혹은 아예 없애기 위함)
        if (_weaponCache.ContainsKey(data))
        {
            _weaponCache.Remove(data);
        }

        // 오브젝트 파괴
        Destroy(_currentWeaponInstance.gameObject);
        _currentWeaponInstance = null;
    }

    public void EquipWeapon(WeaponData newWeaponData)
    {
        if (_isSwapping || newWeaponData == null) return;
        if (_currentWeaponInstance != null && _currentWeaponInstance.BaseData == newWeaponData) return;

        StartCoroutine(SwapRoutine(newWeaponData));
    }

    public void UnequipWeapon()
    {
        if (_currentWeaponInstance != null)
        {
            _currentWeaponInstance.gameObject.SetActive(false);

            Rigidbody rb = _currentWeaponInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
            }

            _currentWeaponInstance = null;
        }
    }

    private System.Collections.IEnumerator SwapRoutine(WeaponData newWeaponData)
    {
        _isSwapping = true;

        if (_animator != null) _animator.SetTrigger("DoSwap");

        if (_currentWeaponInstance != null)
        {
            _currentWeaponInstance.gameObject.SetActive(false);
            Rigidbody oldRb = _currentWeaponInstance.GetComponent<Rigidbody>();
            if (oldRb != null) oldRb.isKinematic = false;
        }

        yield return new WaitForSeconds(0.2f);

        Weapon newWeapon = null;

        // 캐시 확인
        if (_weaponCache.ContainsKey(newWeaponData) && _weaponCache[newWeaponData] != null)
        {
            newWeapon = _weaponCache[newWeaponData];
            newWeapon.gameObject.SetActive(true);
        }
        else
        {
            if (newWeaponData.weaponPrefab != null)
            {
                GameObject weaponObj = Instantiate(newWeaponData.weaponPrefab, _weaponHolder);
                weaponObj.transform.localPosition = Vector3.zero;
                weaponObj.transform.localRotation = newWeaponData.weaponPrefab.transform.localRotation;

                newWeapon = weaponObj.GetComponent<Weapon>();

                if (newWeapon == null)
                {
                    switch (newWeaponData.weaponType)
                    {
                        case WeaponType.Melee: newWeapon = weaponObj.AddComponent<MeleeWeapon>(); break;
                        case WeaponType.Ranged: newWeapon = weaponObj.AddComponent<RangedWeapon>(); break;
                        case WeaponType.Throwable: newWeapon = weaponObj.AddComponent<ThrowableWeapon>(); break;
                    }
                }

                if (newWeapon != null)
                {
                    newWeapon.Initialize(newWeaponData, _playerFirePoint);

                    if (_weaponCache.ContainsKey(newWeaponData))
                        _weaponCache[newWeaponData] = newWeapon;
                    else
                        _weaponCache.Add(newWeaponData, newWeapon);
                }
            }
        }

        _currentWeaponInstance = newWeapon;

        if (_currentWeaponInstance != null)
        {
            ItemHighlighter highlighter = _currentWeaponInstance.GetComponent<ItemHighlighter>();
            if (highlighter != null) highlighter.enabled = false;

            Rigidbody rb = _currentWeaponInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        yield return new WaitForSeconds(0.1f);
        _isSwapping = false;
    }
}