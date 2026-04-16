using UnityEngine;

/// <summary>
/// 경험치 구슬의 로직(탐지, 이동, 획득)을 담당하는 클래스입니다.
/// 플레이어 추적 시 시간이 지날수록 가속도가 붙어 확실하게 흡수되도록 개선되었습니다.
/// </summary>
public class ExpOrb : ParentOrb
{
    [Header("Basic Settings")]
    [SerializeField] private int _expAmount = 10;

    [Header("Audio")]
    [SerializeField] private AudioClip _expSound;

    protected override void ApplyEffect(Player player)
    {
            player.GainExp(_expAmount);

        // [Change] PlaySFX -> PlayExpSFX 로 변경
        // 피치 랜덤값은 이제 매니저가 알아서 계산하므로 넘길 필요 없음
        if (GlobalAudioManager.Instance != null && _expSound != null)
        {
            GlobalAudioManager.Instance.PlayExpSFX(_expSound);
        }
    }
}