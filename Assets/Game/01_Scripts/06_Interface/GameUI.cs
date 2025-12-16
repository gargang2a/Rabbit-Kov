using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("Ammo UI References")]
    public GameObject ammoPanel;
    public TMP_Text curAmmoText;      // 현재 탄알 (큰 숫자)
    public TMP_Text maxAmmoText;      // 전체 탄알 (작은 숫자)

    [Header("Script References")]
    public PlayerWeaponEquipment equipment;

    public static GameUI instance;
    private void Awake() { if (instance == null) instance = this; }

    private void Update()
    {
        UpdateAmmoUI();
    }

    public void UpdateWeaponText(string name)
    {
        // (무기 이름 표시 기능이 있다면 유지, 없으면 비워둠)
    }

    void UpdateAmmoUI()
    {
        if (equipment == null || equipment.CurrentWeapon == null)
        {
            if (ammoPanel != null) ammoPanel.SetActive(false);
            return;
        }

        if (ammoPanel != null) ammoPanel.SetActive(true);

        Weapon currentWeapon = equipment.CurrentWeapon;

        if (currentWeapon.Type == Weapon.WeaponType.Melee)
        {
            if (ammoPanel != null) ammoPanel.SetActive(false);
        }
        else
        {
            // ★ [수정됨] 재장전 상태에 따른 텍스트 및 크기 변경
            if (currentWeapon.IsReloading)
            {
                if (curAmmoText != null)
                {
                    curAmmoText.text = "Reloading...";
                    curAmmoText.fontSize = 60; // 글자가 기니까 작게 축소
                }
            }
            else
            {
                if (curAmmoText != null)
                {
                    curAmmoText.text = currentWeapon.CurAmmo.ToString();
                    curAmmoText.fontSize = 60; // 원래 크기로 복구 (인스펙터 설정값에 맞게 조절하세요)
                }
            }

            if (maxAmmoText != null)
                maxAmmoText.text = currentWeapon.MaxAmmo.ToString();
        }
    }
}