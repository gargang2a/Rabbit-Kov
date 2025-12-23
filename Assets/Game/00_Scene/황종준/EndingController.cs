using UnityEngine;
using UnityEngine.Video; // ★ 비디오 기능을 쓰기 위해 필수
using UnityEngine.SceneManagement;

public class EndingController : MonoBehaviour
{
    [Header("Components")]
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    [Header("Settings")]
    [Tooltip("영상이 끝나면 이동할 씬 이름")]
    public string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        // 1. 이전 씬에서 넘어온 BGM 끄기 (아주 중요)
        if (SoundManager.instance != null)
        {
            SoundManager.instance.StopAllLoopSound();
        }

        // 2. 비디오 및 오디오 동시 재생
        if (videoPlayer != null && audioSource != null)
        {
            // 이벤트 연결
            videoPlayer.loopPointReached += OnVideoFinished;

            // ★ 비디오 준비가 완료되면 재생 (버퍼링 고려)
            videoPlayer.prepareCompleted += (vp) =>
            {
                vp.Play();
                audioSource.Play(); // ★ 여기서 같이 재생!
            };

            videoPlayer.Prepare(); // 준비 시작
        }
    }

    // 영상 재생이 끝났을 때 유니티가 자동으로 호출해주는 함수
    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("엔딩 영상 종료. 메인 메뉴로 복귀합니다.");
        GoToMainMenu();
    }

    private void GoToMainMenu()
    {
        // 메인 메뉴로 이동
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
