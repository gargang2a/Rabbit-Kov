using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems; // ★ [중요] UI 클릭 감지를 위해 필수!

public class PlayerAttack : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _staminaCost = 0;

    [Header("References")]
    public PlayerWeaponEquipment equipment;
    public PlayerController playerController;
    public Animator anim;
    private Player _playerStats;

    // State
    private bool _isFireReady = true;
    private bool _isAttacking = false;

    public bool IsAttacking => _isAttacking;

    private void Awake()
    {
        if (equipment == null) equipment = GetComponent<PlayerWeaponEquipment>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (anim == null) anim = GetComponent<Animator>();
        _playerStats = GetComponent<Player>();
    }

    void Update()
    {
        if (equipment.CurrentWeapon == null) return;

        // ★ [핵심 추가] 마우스가 UI(버튼, 패널 등) 위에 있다면 공격하지 않음
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // 공격 입력
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

    // ... (나머지 TryAttack, TryReload, AttackRoutine 함수는 기존과 동일) ...
    private void TryAttack()
    {
        if (equipment.IsSwapping || playerController.IsRolling || _isAttacking) return;

        Weapon currentWeapon = equipment.CurrentWeapon;

        if (currentWeapon.IsAttacking) return;
        if (currentWeapon.Type == Weapon.WeaponType.Range && currentWeapon.CurAmmo <= 0) return;

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

        if (currentWeapon.Type == Weapon.WeaponType.Melee || currentWeapon.CurAmmo >= currentWeapon.MaxAmmo) return;

        anim.SetTrigger("DoReload");
        currentWeapon.Reload();
    }

    private IEnumerator AttackRoutine(Weapon weapon)
    {
        _isFireReady = false;
        _isAttacking = true;

        string triggerName = (weapon.Type == Weapon.WeaponType.Melee) ? "DoSwing" : "DoShot";
        anim.SetTrigger(triggerName);

        weapon.Use();

        yield return new WaitForSeconds(weapon.CoolTime);

        _isAttacking = false;
        _isFireReady = true;
    }
}