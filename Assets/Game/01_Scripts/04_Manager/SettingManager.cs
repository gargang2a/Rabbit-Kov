using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // [필수] 씬 정보를 가져오기 위해 추가

public class SettingManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider soundSlider;
    public Dropdown resolutionDropdown;
    public GameObject settingPanel;

    // [Style] 네이밍 컨벤션 준수: 내부 클래스 필드 camelCase
    [System.Serializable]
    public class ResolutionItem
    {
        public string label;
        public int width;
        public int height;
    }

    [Header("해상도 목록 설정")]
    public List<ResolutionItem> resolutions = new List<ResolutionItem>();

    // [추가] 씬 이름 관리를 위한 상수 (프로젝트의 실제 씬 이름으로 변경 필요)
    private const string SCENE_MAIN_MENU = "00_Main"; // 예: Lobby, MainMenu 등
    // private const string SCENE_GAME = "GameScene"; // 필요 시 사용

    void Start()
    {
        if (soundSlider != null)
        {
            soundSlider.value = AudioListener.volume;
            // [Style] 이벤트 리스너는 코드에서 연결하는 것이 안전함
            soundSlider.onValueChanged.AddListener(SetVolume);
        }

        InitResolutionOptions();
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    void InitResolutionOptions()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Count; i++)
        {
            options.Add(resolutions[i].label);

            if (resolutions[i].width == Screen.width &&
                resolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // [Style] Dropdown 이벤트 연결
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int index)
    {
        if (index < 0 || index >= resolutions.Count) return;

        ResolutionItem selectedResolution = resolutions[index];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, true);
    }

    public void OpenSettingPanel()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
        }
    }

    // ★ [수정됨] 씬에 따라 동작이 달라지는 종료 로직
    public void CloseSettingPanel()
    {
        // 0. 현재 씬 이름 가져오기
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 1. 설정 창 끄기 로직 분기
        // 요청사항: "메인화면 씬에서는 작동하면 안되고, 전투 씬에서는 작동해야 함"

        if (currentSceneName != SCENE_MAIN_MENU)
        {
            // 메인 메뉴가 아닐 때(즉, 전투/인게임 등)는 여기서 패널을 끈다.
            if (settingPanel != null)
            {
                settingPanel.SetActive(false);
                GameManager.Instance.mainButtonGroup.SetActive(true);
            }
        }
        else
        {
            // 메인 메뉴일 때는 여기서 끄지 않음 (다른 연출이나 로직이 있다고 가정)
            // 필요하다면 이곳에 메인 메뉴 전용 종료 로직 작성
            // Debug.Log("Main Menu: Setting Panel closing handled by other logic.");
        }

        // 2. 메인 메뉴 버튼 복구 로직
        // 이 로직은 논리적으로 '메인 메뉴'에서만 필요할 가능성이 높으므로 씬 체크를 추가하는 것이 안전합니다.
        if (currentSceneName == SCENE_MAIN_MENU)
        {
            if (GameManager.Instance != null && GameManager.Instance.mainButtonGroup != null)
            {
                GameManager.Instance.mainButtonGroup.SetActive(true);
            }
        }
    }
}