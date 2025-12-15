using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // ★ TextMeshPro 필수

public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text ammoText; // 화면에 "30 / 30" 띄울 텍스트

    [Header("Script References")]
    public PlayerWeaponEquipment equipment; // 무기 정보를 얻기 위해 장비 스크립트 연결

    private void Update()
    {
        UpdateAmmoUI();
    }

    void UpdateAmmoUI()
    {
        // 1. 장비 스크립트나 현재 무기가 없으면 텍스트 비우기
        if (equipment == null || equipment.CurrentWeapon == null)
        {
            ammoText.text = "";
            return;
        }

        Weapon currentWeapon = equipment.CurrentWeapon;

        // 2. 무기 타입에 따라 다르게 표시
        if (currentWeapon.Type == Weapon.WeaponType.Melee)
        {
            // 근접 무기면 탄창 표시 안 함 (혹은 "- / -"로 표시)
            ammoText.text = "";
        }
        else // 원거리 무기 (Range)
        {
            // "현재탄알 / 최대탄알" 형식으로 표시
            ammoText.text = $"{currentWeapon.CurAmmo} / {currentWeapon.MaxAmmo}";

            // (선택) 재장전 중이면 "Reloading..." 표시
            if (currentWeapon.IsReloading)
            {
                ammoText.text = "Reloading...";
            }
        }
    }
}