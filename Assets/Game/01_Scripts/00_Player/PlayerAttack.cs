using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems; // ★ UI 클릭 방지용

public class PlayerAttack : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _staminaCost = 15;

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

        // ★ 마우스가 UI 위에 있으면 공격 안함
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetButton("Fire1") && _isFireReady)
        {
            TryAttack();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
        }
    }

    private void TryAttack()
    {
        if (equipment.IsSwapping || playerController.IsRolling || _isAttacking) return;

        Weapon currentWeapon = equipment.CurrentWeapon;

        if (currentWeapon.IsAttacking) return;
        if (currentWeapon.Type == Weapon.WeaponType.Range && currentWeapon.CurAmmo <= 0) return;

        // 스태미너 체크
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