using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    public static GameUI instance;

    [Header("Ammo UI References")]
    public GameObject ammoPanel;      // 탄약 UI 전체 패널
    public TMP_Text curAmmoText;      // 현재 탄알 (큰 숫자)
    public TMP_Text maxAmmoText;      // 전체 탄알 (작은 숫자)

    [Header("Script References")]
    public PlayerWeaponController weaponController; // ★ 교체됨: Equipment -> Controller

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        // 만약 인스펙터에서 할당 안 했다면 자동으로 찾기
        if (weaponController == null)
            weaponController = FindObjectOfType<PlayerWeaponController>();
    }

    private void Update()
    {
        UpdateAmmoUI();
    }

    void UpdateAmmoUI()
    {
        // 1. 컨트롤러나 무기가 없으면 UI 숨김
        if (weaponController == null || weaponController.CurrentWeapon == null)
        {
            if (ammoPanel != null) ammoPanel.SetActive(false);
            return;
        }

        // 2. 현재 무기가 '원거리 무기(RangedWeapon)'인지 확인
        // (is 키워드를 사용하면 형변환과 검사를 동시에 할 수 있습니다)
        if (weaponController.CurrentWeapon is RangedWeapon gun)
        {
            // -> 원거리 무기라면 UI 표시
            if (ammoPanel != null) ammoPanel.SetActive(true);

            // 3. 텍스트 갱신
            if (gun.IsReloading)
            {
                if (curAmmoText != null)
                {
                    curAmmoText.text = "Reloading..";
                    curAmmoText.fontSize = 40; // 글자가 기니까 사이즈 조절 (필요 시 수정)
                }
            }
            else
            {
                if (curAmmoText != null)
                {
                    curAmmoText.text = gun.CurrentAmmo.ToString();
                    curAmmoText.fontSize = 80; // 원래 크기
                }
            }

            if (maxAmmoText != null)
            {
                maxAmmoText.text = $"{gun.MaxAmmo}";
            }
        }
        else
        {
            // -> 근접 무기(MeleeWeapon)라면 탄약 UI 숨김
            if (ammoPanel != null) ammoPanel.SetActive(false);
        }
    }
}