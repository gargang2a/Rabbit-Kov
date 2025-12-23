using UnityEngine;
using UnityEngine.SceneManagement; // ★ 씬 이동 기능을 쓰기 위해 필수인 '네임스페이스'

public class MainMenuController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("이동할 씬의 정확한 이름을 적으세요 (예: LoadingScene)")]
    public string loadingSceneName = "LoadingScene";
    public string gameSceneName = "GameScene";
    // 주의: 여기에는 'GameScene'이 아니라, 중간 다리 역할을 하는 'LoadingScene'의 이름을 적어야 합니다.

    // ★ public이 있어야 드롭다운 메뉴에 뜹니다!
    public void OnClickNewGame()
    {
        // 1. 메인 메뉴 소리 끄기
        if (SoundManager.instance != null)
        {
            SoundManager.instance.StopAllLoopSound();
        }

        // 2. 버튼 클릭 효과음 (선택)
        if (SoundManager.instance != null)
        {
            SoundManager.instance.OnClickButton();
        }

        SceneLoader.TargetSceneName = gameSceneName;
        SceneLoader.CurrentTheme = LoadingTheme.MainToBattle;
        // 3. 씬 이동 (유니티 표준 기능 사용)
        // 팀원들이 만든 LoadingUIController는 'LoadingScene'이 열리면 자동으로 시작될 겁니다.
        SceneManager.LoadScene(loadingSceneName);
    }

    public void OnClickExit()
    {
        if (SoundManager.instance != null) SoundManager.instance.OnClickButton();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}