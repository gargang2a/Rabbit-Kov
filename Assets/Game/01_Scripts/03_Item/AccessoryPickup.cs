using UnityEngine;

public class AccessoryPickup : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("감소시킬 탄퍼짐 각도")]
    [SerializeField] private float _spreadReductionAmount = 2.0f;

    [Header("Audio")]
    [SerializeField] private AudioClip _pickupSound; // [New]
    [Range(0f, 0.5f)]
    [SerializeField] private float _pitchRandomness = 0.1f; // [New]

    private void Update()
    {
        transform.Rotate(Vector3.up * 50f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var weaponController = other.GetComponent<PlayerWeaponController>();
            if (weaponController == null) weaponController = other.GetComponentInParent<PlayerWeaponController>();

            if (weaponController != null)
            {
                Weapon currentWeapon = weaponController.CurrentWeapon;
                if (currentWeapon is RangedWeapon rangedWeapon)
                {
                    rangedWeapon.UpgradeGrip(_spreadReductionAmount);

                    // ★ 사운드 재생 추가
                    if (GlobalAudioManager.Instance != null && _pickupSound != null)
                    {
                        GlobalAudioManager.Instance.PlaySFX(_pickupSound, _pitchRandomness);
                    }

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