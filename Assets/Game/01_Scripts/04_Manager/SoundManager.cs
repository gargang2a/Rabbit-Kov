using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // ★ 다른 스크립트에서 쉽게 부를 수 있게 싱글톤(instance) 만들기
    public static SoundManager instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip mainBgm;
    public AudioClip buttonClick;

    private void Awake()
    {
        // 싱글톤 설정 (이 부분이 있어야 ExpOrb에서 부를 수 있음)
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (mainBgm != null) PlayBGM(mainBgm);
    }

    public void PlayBGM(AudioClip clip)
    {
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // ★ ExpOrb에서 호출하는 함수
    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    public void OnClickButton()
    {
        if (buttonClick != null) sfxSource.PlayOneShot(buttonClick);
    }
}