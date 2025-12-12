using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponEquipment : MonoBehaviour
{
    public GameObject[] weapons;
    public bool[] hasWeapons;

    // 공격 스크립트에서 접근할 수 있게 public으로 변경 (혹은 프로퍼티 사용)
    public GameObject equipWeapon;
    public bool isSwapping = false; // 교체 중인지 확인하는 플래그

    public PlayerController playerController;
    public Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        // 교체 중이거나 구르는 중이면 입력을 받지 않음
        if (isSwapping || playerController.isRolling) return;

        int weaponIndex = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) weaponIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) weaponIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) weaponIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) weaponIndex = 3;

        // 키가 눌렸고, 해당 무기를 가지고 있다면 교체 실행
        if (weaponIndex != -1 && hasWeapons[weaponIndex])
        {
            StartCoroutine(SwapCoroutine(weaponIndex));
        }
    }

    IEnumerator SwapCoroutine(int index)
    {
        isSwapping = true;
        anim.SetTrigger("DoSwap");

        // 기존 무기 숨기기
        if (equipWeapon != null) equipWeapon.SetActive(false);

        // 새 무기 설정
        equipWeapon = weapons[index];

        // 교체 애니메이션 시간 대기
        yield return new WaitForSeconds(0.3f);

        // 새 무기 보이기
        equipWeapon.SetActive(true);

        isSwapping = false;
    }
}

