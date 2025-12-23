using UnityEngine;

public class AccessoryPickup : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("감소시킬 탄퍼짐 각도")]
    [SerializeField] private float _spreadReductionAmount = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip _pickupSound;
    [Range(0f, 0.5f)]
    [SerializeField] private float _pitchRandomness = 0.1f;

    private void Update()
    {
        transform.Rotate(Vector3.up * 50f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. 무기 컨트롤러 찾기
            var weaponController = other.GetComponent<PlayerWeaponController>();
            if (weaponController == null) weaponController = other.GetComponentInParent<PlayerWeaponController>();

            if (weaponController != null)
            {
                // 2. 현재 들고 있는 무기가 '원거리 무기(총)'인지 확인
                Weapon currentWeapon = weaponController.CurrentWeapon;

                if (currentWeapon is RangedWeapon rangedWeapon)
                {
                    // ★ [Fix] 플레이어가 아니라 '총'을 업그레이드 함
                    rangedWeapon.UpgradeGrip(_spreadReductionAmount);

                    Debug.Log($"[{rangedWeapon.BaseData.itemName}]에 수직 손잡이 장착! 반동 {_spreadReductionAmount} 감소.");

                    // 3. 사운드 재생
                    if (GlobalAudioManager.Instance != null && _pickupSound != null)
                    {
                        GlobalAudioManager.Instance.PlaySFX(_pickupSound, _pitchRandomness);
                    }

                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log("현재 총을 들고 있지 않습니다. (습득 실패)");
                }
            }
        }
    }
}