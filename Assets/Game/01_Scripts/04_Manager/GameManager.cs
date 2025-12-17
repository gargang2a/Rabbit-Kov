using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Convention: Static instance는 PascalCase 권장

    [Header("UI References")]
    public GameObject pausePanel;
    public SettingManager settingManager;
    public GameObject exitPanel;

    [Header("Button Groups")]
    public GameObject mainButtonGroup; // Resume, Settings, Quit 버튼 묶음

    // 내부 상태 변수
    public bool isPaused = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // 초기화
        if (pausePanel != null) pausePanel.SetActive(false);
        if (exitPanel != null) exitPanel.SetActive(false);
        if (mainButtonGroup != null) mainButtonGroup.SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 1. 종료 확인 창이 켜져 있다면 -> 종료 취소
            if (exitPanel != null && exitPanel.activeSelf)
            {
                OnCancelQuit();
                return;
            }

            // 2. 설정 창이 켜져 있다면 -> 설정 닫기 (★수정됨)
            if (settingManager != null && settingManager.settingPanel.activeSelf)
            {
                OnCloseSettings(); // 단순히 닫는게 아니라 메인 버튼 복구까지 수행
                return;
            }

            // 3. 아무것도 안 켜져 있다면 -> 일시정지 토글
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            pausePanel.SetActive(true);

            // 일시정지 진입 시: 메인 버튼은 보이고, 종료 창은 숨김
            if (mainButtonGroup != null) mainButtonGroup.SetActive(true);
            if (exitPanel != null) exitPanel.SetActive(false);

            Time.timeScale = 0f;
        }
        else
        {
            pausePanel.SetActive(false);
            // 나갈 때는 모든 서브 패널 닫기
            if (exitPanel != null) exitPanel.SetActive(false);
            if (settingManager != null) settingManager.CloseSettingPanel();

            Time.timeScale = 1f;
        }
    }

    // --- UI Event Functions ---

    public void OnClickResume()
    {
        TogglePause();
    }

    // ★ [수정] 설정 버튼 클릭 시
    public void OnClickSettings()
    {
        if (settingManager != null)
        {
            settingManager.OpenSettingPanel();
        }

        // ★ 핵심: 설정 창을 열면서 메인 버튼 그룹을 숨김
        if (mainButtonGroup != null)
        {
            mainButtonGroup.SetActive(false);
        }
    }

    // ★ [추가] 설정 창 닫기 (ESC 키 또는 설정 창 내부의 '뒤로가기' 버튼에서 호출)
    public void OnCloseSettings()
    {
        if (settingManager != null)
        {
            settingManager.CloseSettingPanel();
        }

        // ★ 핵심: 설정 창이 닫히면 메인 버튼 그룹을 다시 보여줌
        if (mainButtonGroup != null)
        {
            mainButtonGroup.SetActive(true);
        }
    }

    // --- Quit Logic ---

    public void OnClickQuit()
    {
        if (exitPanel != null) exitPanel.SetActive(true);
        if (mainButtonGroup != null) mainButtonGroup.SetActive(false);
    }

    public void OnCancelQuit()
    {
        if (exitPanel != null) exitPanel.SetActive(false);
        if (mainButtonGroup != null) mainButtonGroup.SetActive(true);
    }

    public void OnConfirmQuit()
    {
        Debug.Log("Game Quit");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}