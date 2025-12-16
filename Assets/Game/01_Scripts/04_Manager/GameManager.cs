using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI References")]
    public GameObject pausePanel;
    public SettingManager settingManager;
    public GameObject exitPanel;

    // ★ [추가됨] 메인 버튼 그룹 (Resume, Settings, Quit 묶음)
    public GameObject mainButtonGroup;

    public bool isPaused = false;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (exitPanel != null) exitPanel.SetActive(false);

        // 시작할 때 버튼 그룹은 켜져 있어야 함 (일시정지 하면 바로 보여야 하니까)
        if (mainButtonGroup != null) mainButtonGroup.SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (exitPanel != null && exitPanel.activeSelf)
            {
                OnCancelQuit();
                return;
            }

            if (settingManager != null && settingManager.settingPanel.activeSelf)
            {
                settingManager.CloseSettingPanel();
                return;
            }

            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            pausePanel.SetActive(true);

            // ★ 일시정지 켜질 때: 버튼 그룹은 켜고, 종료 창은 끄기 (초기화)
            if (mainButtonGroup != null) mainButtonGroup.SetActive(true);
            if (exitPanel != null) exitPanel.SetActive(false);

            Time.timeScale = 0f;
        }
        else
        {
            pausePanel.SetActive(false);
            if (exitPanel != null) exitPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    // ... (OnClickResume, OnClickSettings 등 기존 함수 유지) ...
    public void OnClickResume() { TogglePause(); }
    public void OnClickSettings() { if (settingManager != null) settingManager.OpenSettingPanel(); }


    // ==========================================
    // ★ [수정됨] 버튼 끄고 켜는 로직 추가
    // ==========================================

    // Quit 버튼 눌렀을 때
    public void OnClickQuit()
    {
        if (exitPanel != null) exitPanel.SetActive(true); // 종료 창 켜기

        // ★ 버튼 그룹 숨기기
        if (mainButtonGroup != null) mainButtonGroup.SetActive(false);
    }

    // "아니" 눌렀을 때 (취소)
    public void OnCancelQuit()
    {
        if (exitPanel != null) exitPanel.SetActive(false); // 종료 창 끄기

        // ★ 버튼 그룹 다시 보이기
        if (mainButtonGroup != null) mainButtonGroup.SetActive(true);
    }

    // "응" 눌렀을 때 (진짜 종료) - 기존 유지
    public void OnConfirmQuit()
    {
        Debug.Log("게임 종료!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}