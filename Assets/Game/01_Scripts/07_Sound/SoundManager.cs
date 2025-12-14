using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource; // 배경음악용 (Loop 체크)
    public AudioSource sfxSource; // 효과음용 (Loop 해제)

    [Header("Audio Clips")]
    public AudioClip mainBgm;     // 메인 배경음악
    public AudioClip buttonClick; // 버튼 클릭음

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        // 게임 시작 시 BGM 재생
        PlayBGM(mainBgm);
    }

    public void PlayBGM(AudioClip clip)
    {
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    // 버튼에서 호출할 함수
    public void OnClickButton()
    {
        sfxSource.PlayOneShot(buttonClick);
    }
}