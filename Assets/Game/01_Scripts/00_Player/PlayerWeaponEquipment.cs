using System.Collections;
using UnityEngine;

public class PlayerWeaponEquipment : MonoBehaviour
{
    [Header("Inventory")]
    // [Optimization] GameObject 대신 Weapon 컴포넌트를 직접 참조하여 캐싱 비용 절약
    public Weapon[] weapons;
    public bool[] hasWeapons;

    [Header("State")]
    [SerializeField] private Weapon _currentWeapon;
    private bool _isSwapping = false;

    // References
    public PlayerController playerController;
    public Animator anim;

    // Properties
    public Weapon CurrentWeapon => _currentWeapon;
    public bool IsSwapping => _isSwapping;

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_isSwapping || playerController.IsRolling) return;

        // 입력 처리 (간소화)
        int weaponIndex = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) weaponIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) weaponIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) weaponIndex = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) weaponIndex = 3;

        if (weaponIndex != -1)
        {
            // 배열 범위 체크 및 소유 여부 확인
            if (weaponIndex < weapons.Length && hasWeapons[weaponIndex])
            {
                StartCoroutine(SwapCoroutine(weaponIndex));
            }
        }
    }

    private IEnumerator SwapCoroutine(int index)
    {
        _isSwapping = true;
        anim.SetTrigger("DoSwap");

        // 기존 무기 비활성화
        if (_currentWeapon != null)
            _currentWeapon.gameObject.SetActive(false);

        // 새 무기 교체
        _currentWeapon = weapons[index];

        // 교체 애니메이션 대기
        yield return new WaitForSeconds(0.3f);

        // 새 무기 활성화
        if (_currentWeapon != null)
            _currentWeapon.gameObject.SetActive(true);

        _isSwapping = false;
    }
}