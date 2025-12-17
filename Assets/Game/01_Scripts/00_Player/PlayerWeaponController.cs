using UnityEngine;
using UnityEngine.EventSystems; // UI 클릭 방지용

public class PlayerWeaponController : MonoBehaviour
{
    [Header("Test")]
    public WeaponData testWeapon;

    [Header("Settings")]
    [SerializeField] private Transform _weaponHolder; // 무기가 생성될 부모(오른손) Transform

    [Header("Override")]
    [SerializeField] private Transform _playerFirePoint; // 플레이어 기준 발사 위치 (필요 시 할당)

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Player _playerStats;     // 스태미너 체크용
    [SerializeField] private PlayerController _playerController; // 구르기 상태 확인용

    [Header("State")]
    private Weapon _currentWeaponInstance; // 현재 손에 들린 무기 객체
    private bool _isSwapping = false;

    // 외부에서 현재 무기 정보가 필요할 때 접근
    public Weapon CurrentWeapon => _currentWeaponInstance;

    private void Awake()
    {
        // 컴포넌트 자동 할당 (없으면 수동 할당 필요)
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_playerStats == null) _playerStats = GetComponent<Player>();
        if (_playerController == null) _playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        // 테스트용 무기 장착
        if (testWeapon != null)
        {
            EquipWeapon(testWeapon);
        }
    }

    private void Update()
    {
        // 1. 예외 처리: 무기가 없거나, 교체 중이거나, 구르는 중이면 조작 불가
        if (_currentWeaponInstance == null || _isSwapping) return;
        if (_playerController != null && _playerController.IsRolling) return;

        // 2. UI 클릭 방지 (인벤토리 정리 중 발사 방지)
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // 3. 공격 입력 (누르고 있는 동안 연사)
        if (Input.GetButton("Fire1"))
        {
            TryAttack();
        }

        // 4. 재장전 입력 (R키)
        if (Input.GetKeyDown(KeyCode.R))
        {
            _currentWeaponInstance.Reload();
            // 재장전 애니메이션
            if (_animator != null) _animator.SetTrigger("DoReload");
        }
    }

    private void TryAttack()
    {
        // ★ [핵심 수정] 무기가 쿨타임 중(준비 안됨)이면 즉시 리턴!
        // 이 코드가 없으면 버튼을 누르는 동안 애니메이션 트리거가 계속 쌓여서 공격이 여러 번 나갑니다.
        if (!_currentWeaponInstance.IsReady) return;

        // 2. 스태미너 체크 (근접 무기일 경우)
        if (_currentWeaponInstance is MeleeWeapon)
        {
            // 근접 공격 시 스태미너 부족하면 공격 불가
            if (_playerStats != null && _playerStats.Stamina < 10) return;

            // 스태미너 소모 (Player 스크립트에 UseStamina 함수가 있다면 사용)
            // _playerStats.UseStamina(10); 
        }

        // 3. 애니메이션 트리거 실행
        if (_animator != null)
        {
            // 무기 타입에 따라 다른 트리거 발동
            if (_currentWeaponInstance is RangedWeapon)
            {
                // 원거리 무기: 탄알이 있을 때만 애니메이션 재생
                var ranged = _currentWeaponInstance as RangedWeapon;
                if (ranged != null && ranged.HasAmmo)
                {
                    _animator.SetTrigger("DoShot");
                }
            }
            else if (_currentWeaponInstance is MeleeWeapon)
            {
                // 근접 무기
                _animator.SetTrigger("DoSwing");
            }
        }

        // 4. 실제 로직 실행 (총알 발사 / 칼 휘두르기)
        _currentWeaponInstance.Use();
    }

    // ====================================================
    // ★ 핵심: 인벤토리 시스템에서 호출할 무기 장착 함수
    // ====================================================
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

        // 1. 교체 애니메이션 재생 (있다면)
        if (_animator != null) _animator.SetTrigger("DoSwap");

        // 2. 기존 무기 제거
        if (_currentWeaponInstance != null)
        {
            Destroy(_currentWeaponInstance.gameObject);
        }

        // 3. 애니메이션 타이밍 대기 (손을 내리는 시간 등)
        yield return new WaitForSeconds(0.2f);

        // 4. 새 무기 프리팹 생성 (Instantiate)
        if (newWeaponData.weaponPrefab != null)
        {
            GameObject weaponObj = Instantiate(newWeaponData.weaponPrefab, _weaponHolder);

            // 위치는 손 위치(0,0,0)로 초기화
            weaponObj.transform.localPosition = Vector3.zero;

            // ★ [수정됨] 회전은 프리팹에 설정된 값을 따름 (Y축 90도 등 유지)
            weaponObj.transform.localRotation = newWeaponData.weaponPrefab.transform.localRotation;

            // 5. 데이터 주입 (Initialize)
            _currentWeaponInstance = weaponObj.GetComponent<Weapon>();
            if (_currentWeaponInstance != null)
            {
                // 플레이어의 발사 위치(_playerFirePoint)가 있다면 넘겨줌
                _currentWeaponInstance.Initialize(newWeaponData, _playerFirePoint);
            }
        }

        // 6. 교체 완료 대기
        yield return new WaitForSeconds(0.1f);
        _isSwapping = false;
    }
}