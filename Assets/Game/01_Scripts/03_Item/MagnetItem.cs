using UnityEngine;

public class MagnetItem : MonoBehaviour
{
    [Header("Effect Settings")]
    public float rotationSpeed = 100f;

    [Header("Audio")]
    [SerializeField] private AudioClip _pickupSound; // [New] 자석 획득 소리

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ActivateAllOrbs();

            // ★ [GlobalAudioManager] 연동
            if (GlobalAudioManager.Instance != null && _pickupSound != null)
            {
                // 특수 아이템은 피치 변화를 적게(0.05) 주어 또렷하게 들리게 합니다.
                GlobalAudioManager.Instance.PlaySFX(_pickupSound, 0.05f);
            }

            Destroy(gameObject);
        }
    }

    void ActivateAllOrbs()
    {
        // 1. 경험치 구슬 당기기
        ExpOrb[] expOrbs = FindObjectsOfType<ExpOrb>();
        foreach (ExpOrb orb in expOrbs) orb.ActivateMagnet();

        // [Tip] 나중에 HealthOrb나 StaminaOrb도 같이 당기고 싶다면 아래 주석을 해제하세요.
        /*
        HealthOrb[] healthOrbs = FindObjectsOfType<HealthOrb>();
        foreach (HealthOrb orb in healthOrbs) orb.ActivateMagnet();

        StaminaOrb[] staminaOrbs = FindObjectsOfType<StaminaOrb>();
        foreach (StaminaOrb orb in staminaOrbs) orb.ActivateMagnet();
        */

        Debug.Log($"자석 효과 발동! 경험치 {expOrbs.Length}개를 당깁니다.");
    }
}