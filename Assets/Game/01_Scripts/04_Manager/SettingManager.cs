using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider soundSlider;
    public Dropdown resolutionDropdown;
    public GameObject settingPanel;

    // ★ [핵심] 해상도 정보와 이름을 같이 담을 클래스 정의
    [System.Serializable]
    public class ResolutionItem
    {
        public string label;    // 화면에 보일 이름 (예: "1080p")
        public int width;       // 가로 (1920)
        public int height;      // 세로 (1080)
    }

    [Header("해상도 목록 설정")]
    public List<ResolutionItem> resolutions = new List<ResolutionItem>();

    void Start()
    {
        soundSlider.value = AudioListener.volume;
        InitResolutionOptions();
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    void InitResolutionOptions()
    {
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Count; i++)
        {
            // ★ 우리가 적어둔 'label' (예: "1080p")을 드롭다운에 추가
            options.Add(resolutions[i].label);

            // 현재 화면 크기와 비교해서 초기값 설정
            if (resolutions[i].width == Screen.width &&
                resolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void SetResolution(int index)
    {
        // 선택된 인덱스의 해상도 정보 가져오기
        ResolutionItem selectedResolution = resolutions[index];

        // 해상도 적용
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, true);
    }

    public void CloseSettingPanel()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
    }

    // ★ [추가] 세팅 창 열기 함수
    public void OpenSettingPanel()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(true); // 꺼져있던 패널을 다시 켭니다!
        }
    }
}