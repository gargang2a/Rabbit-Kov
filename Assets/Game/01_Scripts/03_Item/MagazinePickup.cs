using UnityEngine;

public class MagazinePickup : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("늘어날 최대 탄약 수 (예: 10발)")]
    [SerializeField] private int _increaseAmount = 10;

    [Header("Audio")]
    [SerializeField] private AudioClip _pickupSound; // [New] 사운드 파일
    [Range(0f, 0.5f)]
    [SerializeField] private float _pitchRandomness = 0.1f; // [New] 피치 랜덤

    private void Update()
    {
        transform.Rotate(Vector3.up * 50f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerWeaponController weaponController = other.GetComponent<PlayerWeaponController>();
            if (weaponController == null) weaponController = other.GetComponentInParent<PlayerWeaponController>();

            if (weaponController != null)
            {
                Weapon currentWeapon = weaponController.CurrentWeapon;

                // RangedWeapon일 때만 획득 가능
                if (currentWeapon is RangedWeapon rangedWeapon)
                {
                    rangedWeapon.UpgradeMagazine(_increaseAmount);

                    // ★ 사운드 재생 추가
                    if (GlobalAudioManager.Instance != null && _pickupSound != null)
                    {
                        GlobalAudioManager.Instance.PlaySFX(_pickupSound, _pitchRandomness);
                    }

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