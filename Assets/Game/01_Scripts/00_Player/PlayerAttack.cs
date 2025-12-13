using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    public PlayerWeaponEquipment equipment;
    public PlayerController playerController;
    public Animator anim;

    // State
    private bool _isFireReady = true;
    private bool _isAttacking = false;

    public bool IsAttacking => _isAttacking;

    void Update()
    {
        if (equipment.CurrentWeapon == null) return;

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

    private void TryAttack()
    {
        // 예외 처리: 교체 중, 구르기 중, 무기 없음
        if (equipment.IsSwapping || playerController.IsRolling) return;

        Weapon currentWeapon = equipment.CurrentWeapon;

        // 무기 자체 상태 확인
        if (currentWeapon.IsAttacking) return;
        // 원거리 무기인데 총알이 없으면 공격 불가
        if (currentWeapon.Type == Weapon.WeaponType.Range && currentWeapon.CurAmmo <= 0) return;

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