using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _staminaCost = 0; // ★ 공격 시 소모할 스태미너

    [Header("References")]
    public PlayerWeaponEquipment equipment;
    public PlayerController playerController;
    public Animator anim;
    private Player _playerStats; // ★ 스태미너 관리를 위한 참조

    // State
    private bool _isFireReady = true;
    private bool _isAttacking = false;

    public bool IsAttacking => _isAttacking;

    private void Awake()
    {
        // 컴포넌트 자동 할당 (인스펙터 누락 방지)
        if (equipment == null) equipment = GetComponent<PlayerWeaponEquipment>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (anim == null) anim = GetComponent<Animator>();

        // ★ Player 스크립트 가져오기
        _playerStats = GetComponent<Player>();
    }

    void Update()
    {
        if (equipment.CurrentWeapon == null) return;

        // 공격 입력 (누르고 있으면 연사)
        if (Input.GetButton("Fire1") && _isFireReady)
        {
            TryAttack();
        }

        // 재장전 입력
        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
        }
    }

    private void TryAttack()
    {
        // 1. 기본 예외 처리: 교체 중, 구르기 중, 공격 중
        if (equipment.IsSwapping || playerController.IsRolling || _isAttacking) return;

        Weapon currentWeapon = equipment.CurrentWeapon;

        // 2. 무기 상태 확인
        if (currentWeapon.IsAttacking) return;
        if (currentWeapon.Type == Weapon.WeaponType.Range && currentWeapon.CurAmmo <= 0) return;

        // ★ 3. 스태미너 체크
        // 스태미너가 부족하면 공격 불가 (UseStamina가 false 반환)
        if (_playerStats != null)
        {
            if (!_playerStats.UseStamina(_staminaCost)) return;
        }

        StartCoroutine(AttackRoutine(currentWeapon));
    }

    private void TryReload()
    {
        if (equipment.IsSwapping || playerController.IsRolling || _isAttacking) return;

        Weapon currentWeapon = equipment.CurrentWeapon;
        if (currentWeapon == null) return;

        // 근접 무기거나 이미 탄창이 꽉 찼으면 리턴
        if (currentWeapon.Type == Weapon.WeaponType.Melee || currentWeapon.CurAmmo >= currentWeapon.MaxAmmo) return;

        anim.SetTrigger("DoReload");
        currentWeapon.Reload();
    }

    private IEnumerator AttackRoutine(Weapon weapon)
    {
        _isFireReady = false;
        _isAttacking = true;

        // 애니메이션 트리거
        string triggerName = (weapon.Type == Weapon.WeaponType.Melee) ? "DoSwing" : "DoShot";
        anim.SetTrigger(triggerName);

        // 무기 로직 실행
        weapon.Use();

        // 쿨타임 대기
        yield return new WaitForSeconds(weapon.CoolTime);

        _isAttacking = false;
        _isFireReady = true;
    }
}