using UnityEngine;

public class MagazinePickup : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("늘어날 최대 탄약 수 (예: 10발)")]
    [SerializeField] private int _increaseAmount = 10;

    private void Update()
    {
        transform.Rotate(Vector3.up * 50f * Time.deltaTime); // 회전 연출
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. 플레이어의 무기 컨트롤러를 찾음
            PlayerWeaponController weaponController = other.GetComponent<PlayerWeaponController>();

            // (혹시 Player 컴포넌트 쪽에 붙어있을 수도 있으니 방어 코드)
            if (weaponController == null)
                weaponController = other.GetComponentInParent<PlayerWeaponController>();

            if (weaponController != null)
            {
                // 2. 현재 들고 있는 무기가 '원거리 무기(RangedWeapon)'인지 확인
                Weapon currentWeapon = weaponController.CurrentWeapon;

                if (currentWeapon is RangedWeapon rangedWeapon)
                {
                    // 3. 탄창 업그레이드 실행
                    rangedWeapon.UpgradeMagazine(_increaseAmount);

                    // 4. 아이템 삭제
                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log("현재 총을 들고 있지 않거나, 근접 무기입니다.");
                }
            }
        }
    }
}