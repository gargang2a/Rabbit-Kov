using UnityEngine;

/// <summary>
/// 체력 회복 구슬 클래스
/// GlobalAudioManager와 연동되어 획득 시 사운드를 재생합니다.
/// </summary>
public class HealthOrb : ParentOrb
{
    [Header("Basic Settings")]
    [Tooltip("회복할 체력량")]
    [SerializeField] private float _healAmount = 20f;
  
    [Header("Audio")]
    [SerializeField] private AudioClip _healSound;

    protected override void ApplyEffect(Player player)
    {
        player.Heal(_healAmount);

        // ★ [GlobalAudioManager] 연동
        if (GlobalAudioManager.Instance != null && _healSound != null)
        {
            // 회복은 기분 좋은 소리이므로 약간의 피치 변화(0.1)를 줍니다.
            GlobalAudioManager.Instance.PlaySFX(_healSound, 0.1f);
        }
    }
}