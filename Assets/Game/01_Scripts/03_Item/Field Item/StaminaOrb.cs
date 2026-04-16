using UnityEngine;

/// <summary>
/// 스태미나 회복 구슬 클래스
/// GlobalAudioManager와 연동되어 획득 시 사운드를 재생합니다.
/// </summary>
public class StaminaOrb : ParentOrb
{
    [Header("Basic Settings")]
    [Tooltip("회복할 스태미나 양")]
    [SerializeField] private float _restoreAmount = 30f;


    [Header("Audio")]
    [SerializeField] private AudioClip _restoreSound;

    protected override void ApplyEffect(Player player)
    {
        player.RestoreStamina(_restoreAmount);

        // ★ [GlobalAudioManager] 연동
        if (GlobalAudioManager.Instance != null && _restoreSound != null)
        {
            GlobalAudioManager.Instance.PlaySFX(_restoreSound, 0.1f);
        }
    }
}