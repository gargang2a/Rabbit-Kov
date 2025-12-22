using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;      // 음악용
    public AudioSource sfxSource;      // 효과음용
    public AudioSource ambientSource;  // ★ [추가] 자연 소리(환경음)용

    [Header("BGM Clips")]
    public AudioClip mainBgm;
    public AudioClip gameBgm;

    [Header("Ambience Clips")]
    public AudioClip natureSound;      // ★ [추가] 자연 소리 파일

    [Header("SFX Clips")]
    public AudioClip buttonClick;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 1. 메인 BGM, 자연 소리 같이 재생
        if (mainBgm != null) PlayBGM(mainBgm);
        if (natureSound != null) PlayAmbience(natureSound);
    }

    /// <summary>
    /// BGM을 즉시 정지합니다.
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource.isPlaying)
        {
            bgmSource.Stop();
            bgmSource.clip = null; // 클립을 비워서 다음 로직 충돌 방지
        }
    }

    /// <summary>
    /// 환경음을 즉시 정지합니다.
    /// </summary>
    public void StopAmbience()
    {
        if (ambientSource.isPlaying)
        {
            ambientSource.Stop();
            ambientSource.clip = null;
        }
    }

    /// <summary>
    /// 씬 전환 시 모든 루프 사운드(BGM, 환경음)를 끕니다.
    /// SFX는 보통 짧으므로 놔두거나 필요시 포함합니다.
    /// </summary>
    public void StopAllLoopSound()
    {
        StopBGM();
        StopAmbience();
    }

    // BGM 재생 (기존 동일)
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // ★ [추가] 환경음 재생 함수
    public void PlayAmbience(AudioClip clip)
    {
        if (clip == null) return;

        // 이미 같은 환경음이 재생 중이면 무시
        if (ambientSource.clip == clip && ambientSource.isPlaying) return;

        ambientSource.clip = clip;
        ambientSource.loop = true; // 자연 소리는 계속 반복되어야 함
        ambientSource.Play();
    }

    // BGM 교체 편의 함수들
    public void PlayGameBGM() => PlayBGM(gameBgm);
    public void PlayMainBGM() => PlayBGM(mainBgm);

    // SFX 재생 (기존 동일)
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
    }

    public void OnClickButton()
    {
        if (buttonClick != null) sfxSource.PlayOneShot(buttonClick);
    }
}