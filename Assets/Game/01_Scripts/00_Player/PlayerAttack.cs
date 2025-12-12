using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    // 장비 스크립트 참조
    public PlayerWeaponEquipment equipment;
    public PlayerController playerController; // 구르기 확인용
    public Animator anim;

    // 공격 쿨타임 관리
    public float fireDelay = 0.5f;
    private bool isFireReady = true;

    void Update()
    {
        // 공격 입력 (좌클릭)
        if (Input.GetButtonDown("Fire1"))
        {
            Attack();
        }
    }

    void Attack()
    {
        // 1. 공격 조건 체크: 무기가 없거나, 교체 중이거나, 구르는 중이거나, 쿨타임 중이면 공격 불가
        if (equipment.equipWeapon == null || equipment.isSwapping || playerController.isRolling || !isFireReady)
            return;

        // 2. 현재 들고 있는 무기의 Weapon 스크립트 가져오기
        Weapon currentWeapon = equipment.equipWeapon.GetComponent<Weapon>();
        if (currentWeapon == null) return; // 무기에 Weapon 스크립트가 안 붙어있으면 리턴

        // 3. 공격 실행 코루틴 시작
        StartCoroutine(AttackRoutine(currentWeapon));
    }

    IEnumerator AttackRoutine(Weapon weapon)
    {
        isFireReady = false; // 쿨타임 시작

        // 애니메이션 실행
        anim.SetTrigger("DoSwing"); // 애니메이터에 DoSwing 트리거 필요

        // 무기 자체의 로직 실행 (콜라이더 켜기 등)
        weapon.Use();

        // 공격 후 딜레이 (쿨타임) 대기
        yield return new WaitForSeconds(fireDelay);

        isFireReady = true; // 다음 공격 준비 완료
    }
}