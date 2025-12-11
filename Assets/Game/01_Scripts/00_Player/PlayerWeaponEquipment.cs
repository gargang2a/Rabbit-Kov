using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponEquipment : MonoBehaviour
{
    public GameObject[] weapons;
    public bool[] hasWeapons;
    private GameObject equipWeapon;

    bool slotDown1;
    bool slotDown2;
    bool slotDown3;
    bool slotDown4;

    public PlayerController playerController;
    public Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        GetInput();
        StartCoroutine(SwapCoroutine());
    }

    void GetInput()
    {
        slotDown1 = Input.GetKeyDown(KeyCode.Alpha1);
        slotDown2 = Input.GetKeyDown(KeyCode.Alpha2);
        slotDown3 = Input.GetKeyDown(KeyCode.Alpha3);
        slotDown4 = Input.GetKeyDown(KeyCode.Alpha4);
    }

    IEnumerator SwapCoroutine()
    {
        int weaponIndex = -1;
        if (slotDown1) weaponIndex = 0;
        if (slotDown2) weaponIndex = 1;
        if (slotDown3) weaponIndex = 2;
        if (slotDown4) weaponIndex = 3;

        if ((slotDown1 || slotDown2 || slotDown3 || slotDown4) && !playerController.isRolling)
        {
            if (equipWeapon != null) equipWeapon.SetActive(false);
            equipWeapon = weapons[weaponIndex];
            anim.SetTrigger("DoSwap");

            yield return new WaitForSeconds(0.3f);
            equipWeapon.SetActive(true);
        }
    }
}


 