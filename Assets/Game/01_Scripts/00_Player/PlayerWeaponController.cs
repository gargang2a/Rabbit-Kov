// PlayerWeaponController.cs 파일 수정 (Rigidbody 제어 로직 추가)

using UnityEngine;
using UnityEngine.EventSystems; // UI 클릭 방지용
using System.Collections.Generic; // Dictionary 사용을 위해 필수

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
            // 1. Destroy 대신 SetActive(false) 사용
            _currentWeaponInstance.gameObject.SetActive(false);

            // ★★★ [수정] 무기 해제 시 RigidBody 비키네마틱으로 전환 (물리 재활성화)
            Rigidbody rb = _currentWeaponInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
            }
            // ★★★ 여기까지 수정

            _currentWeaponInstance = null;
        }
    }

    private System.Collections.IEnumerator SwapRoutine(WeaponData newWeaponData)
    {
        _isSwapping = true;

        if (_animator != null) _animator.SetTrigger("DoSwap");

        // 1. 기존 무기 숨기기
        if (_currentWeaponInstance != null)
        {
            _currentWeaponInstance.gameObject.SetActive(false);

            // ★★★ [수정] 캐시로 돌아가는 무기의 Rigidbody 상태 복구 (드랍/획득을 위해 물리 연산 활성화)
            Rigidbody oldRb = _currentWeaponInstance.GetComponent<Rigidbody>();
            if (oldRb != null)
            {
                oldRb.isKinematic = false;
            }
            // ★★★ 여기까지 수정
        }

        yield return new WaitForSeconds(0.2f); // 무기 교체 애니메이션 대기

        // 2. 새 무기 꺼내기 로직 (캐시 확인)
        Weapon newWeapon = null; // 임시 변수 선언

        // ★ [수정] 키가 존재하고, 실제 오브젝트도 파괴되지 않고 살아있는지 확인
        if (_weaponCache.ContainsKey(newWeaponData) && _weaponCache[newWeaponData] != null)
        {
            // A. 이미 만들었던 무기 -> 켜기
            newWeapon = _weaponCache[newWeaponData];
            newWeapon.gameObject.SetActive(true);
        }
        else
        {
            // B. 처음 드는 무기이거나, 모종의 이유로 삭제된 무기 -> 새로 생성
            if (newWeaponData.weaponPrefab != null)
            {
                GameObject weaponObj = Instantiate(newWeaponData.weaponPrefab, _weaponHolder);
                weaponObj.transform.localPosition = Vector3.zero;
                weaponObj.transform.localRotation = newWeaponData.weaponPrefab.transform.localRotation;

                newWeapon = weaponObj.GetComponent<Weapon>();

                // 스크립트 자동 부착
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

                    // ★ [핵심 수정] 무조건 Add하지 않고, 안전하게 넣기
                    if (_weaponCache.ContainsKey(newWeaponData))
                    {
                        // 이미 키는 있는데 내용물이 비어있던 경우 -> 덮어쓰기
                        _weaponCache[newWeaponData] = newWeapon;
                    }
                    else
                    {
                        // 아예 키가 없는 경우 -> 새로 추가
                        _weaponCache.Add(newWeaponData, newWeapon);
                    }
                }
            }
        }

        // A, B 경로의 결과물을 최종 인스턴스로 지정
        _currentWeaponInstance = newWeapon;

        // ★★★ 3. 장착된 무기 인스턴스 제어 로직
        if (_currentWeaponInstance != null)
        {
            // A. ItemHighlighter 비활성화 (기존 로직 유지)
            ItemHighlighter highlighter = _currentWeaponInstance.GetComponent<ItemHighlighter>();
            if (highlighter != null)
            {
                highlighter.enabled = false;
                Debug.Log($"무기 장착 완료: {newWeaponData.itemName}. ItemHighlighter 비활성화 완료.");
            }

            // B. ★★★ [핵심 추가] RigidBody 키네마틱 설정 (물리 연산 중지)
            Rigidbody rb = _currentWeaponInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // 물리 엔진의 영향을 받지 않음 (플레이어 손을 따라 움직임)
                rb.velocity = Vector3.zero; // 혹시 모를 잔여 물리 연산 초기화
                rb.angularVelocity = Vector3.zero;
            }
        }
        // ★★★ 핵심 수정 끝

        yield return new WaitForSeconds(0.1f);
        _isSwapping = false;
    }
}