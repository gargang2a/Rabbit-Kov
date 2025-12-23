//using UnityEngine;
//using UnityEngine.SceneManagement; // ★ 씬 이동 기능을 쓰기 위해 필수인 '네임스페이스'

//public class MainMenuController : MonoBehaviour
//{
//    [Header("Settings")]
//    [Tooltip("이동할 씬의 정확한 이름을 적으세요 (예: LoadingScene)")]
//    public string loadingSceneName = "LoadingScene";
//    public string gameSceneName = "GameScene";
//    // 주의: 여기에는 'GameScene'이 아니라, 중간 다리 역할을 하는 'LoadingScene'의 이름을 적어야 합니다.

//    // ★ public이 있어야 드롭다운 메뉴에 뜹니다!
//    public void OnClickNewGame()
//    {
//        // 1. 메인 메뉴 소리 끄기
//        if (SoundManager.instance != null)
//        {
//            SoundManager.instance.StopAllLoopSound();
//        }

//        // 2. 버튼 클릭 효과음 (선택)
//        if (SoundManager.instance != null)
//        {
//            SoundManager.instance.OnClickButton();
//        }

//        SceneLoader.TargetSceneName = gameSceneName;
//        SceneLoader.CurrentTheme = LoadingTheme.MainToBattle;
//        // 3. 씬 이동 (유니티 표준 기능 사용)
//        // 팀원들이 만든 LoadingUIController는 'LoadingScene'이 열리면 자동으로 시작될 겁니다.
//        SceneManager.LoadScene(loadingSceneName);
//    }

//    public void OnClickContinue()
//    {
//        // 1. 저장된 파일 불러오기 시도
//        if (DataManager.instance.LoadGame())
//        {
//            // 2. 성공하면 기지 씬(BaseScene)으로 이동
//            // (기지에서 저장했다고 했으므로, 불러오는 곳도 기지여야 자연스럽습니다)
//            SceneLoader.TargetSceneName = "01_Base House"; // 기지 씬 이름
//            SceneLoader.CurrentTheme = LoadingTheme.MainToBase;

//            // 3. 로딩 씬 경유
//            SceneManager.LoadScene("Loading1 Green");
//        }
//        else
//        {
//            // 파일이 없으면 경고음이나 팝업
//            Debug.Log("저장된 게임이 없습니다!");
//            // SoundManager.instance.PlayErrorSound(); 
//        }
//    }

//    public void OnClickExit()
//    {
//        if (SoundManager.instance != null) SoundManager.instance.OnClickButton();

//#if UNITY_EDITOR
//        UnityEditor.EditorApplication.isPlaying = false;
//#else
//        Application.Quit();
//#endif
//    }
//}