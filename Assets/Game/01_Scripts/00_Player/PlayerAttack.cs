using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerAttack : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _staminaCost = 15;

    [Header("Grenade Settings")]
    public GameObject grenadePrefab;
    public Transform throwPoint;
    public float throwForce = 20f;
    public float throwUpwardForce = 10f;
    public int grenadeStaminaCost = 0;

    [Header("References")]
    public PlayerWeaponEquipment equipment;
    public PlayerController playerController;
    public Animator anim;
    private Player _playerStats;

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
        // 1. UI 클릭 중이면 모든 행동 무시 (이건 유지)
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // 2. 기본 공격 (좌클릭) - ★ 무기가 있을 때만 가능
        if (Input.GetButton("Fire1") && _isFireReady)
        {
            if (equipment.CurrentWeapon != null)
            {
                TryAttack();
            }
        }

        // 3. 재장전 (R키) - ★ 무기가 있을 때만 가능
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (equipment.CurrentWeapon != null)
            {
                TryReload();
            }
        }

        // 4. 수류탄 투척 (G키) - ★ 무기가 없어도 가능!
        if (Input.GetKeyDown(KeyCode.G))
        {
            TryThrowGrenade();
        }
    }

    // ... (아래 TryThrowGrenade, TryAttack, TryReload 등 함수 내용은 기존과 동일) ...

    private void TryThrowGrenade()
    {
        if (equipment.IsSwapping || playerController.IsRolling || _isAttacking) return;

        if (_playerStats != null)
        {
            if (!_playerStats.UseStamina(grenadeStaminaCost))
            {
                Debug.Log("수류탄 던질 스태미너 부족!");
                return;
            }
        }

        StartCoroutine(ThrowRoutine());
    }

    private IEnumerator ThrowRoutine()
    {
        _isAttacking = true;
        anim.SetTrigger("DoThrow");

        yield return new WaitForSeconds(0.3f);

        if (grenadePrefab != null && throwPoint != null)
        {
            GameObject grenade = Instantiate(grenadePrefab, throwPoint.position, throwPoint.rotation);
            Rigidbody rb = grenade.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Vector3 force = (transform.forward * throwForce) + (transform.up * throwUpwardForce);
                rb.AddForce(force, ForceMode.Impulse);
            }
        }

        yield return new WaitForSeconds(0.5f);
        _isAttacking = false;
    }

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