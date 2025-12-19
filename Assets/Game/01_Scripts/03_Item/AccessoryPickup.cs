using UnityEngine;

public class AccessoryPickup : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("감소시킬 탄퍼짐 각도")]
    [SerializeField] private float _spreadReductionAmount = 2.0f;

    private void Update()
    {
        transform.Rotate(Vector3.up * 50f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. 플레이어 무기 컨트롤러 찾기
            var weaponController = other.GetComponent<PlayerWeaponController>();
            if (weaponController == null) weaponController = other.GetComponentInParent<PlayerWeaponController>();

            if (weaponController != null)
            {
                // 2. 현재 들고 있는 총이 원거리 무기인지 확인
                Weapon currentWeapon = weaponController.CurrentWeapon;
                if (currentWeapon is RangedWeapon rangedWeapon)
                {
                    // 3. 총기 자체를 업그레이드 (이 데이터는 이제 사라지지 않음!)
                    rangedWeapon.UpgradeGrip(_spreadReductionAmount);

                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log("현재 원거리 무기를 들고 있지 않습니다.");
                }
            }
        }
    }
}