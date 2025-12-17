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

    [System.Serializable]
    public class ResolutionItem
    {
        public string label;
        public int width;
        public int height;
    }

    [Header("해상도 목록 설정")]
    public List<ResolutionItem> resolutions = new List<ResolutionItem>();

    void Start()
    {
        // 오디오 리스너가 없으면 에러가 날 수 있으므로 체크 권장
        if (soundSlider != null) soundSlider.value = AudioListener.volume;
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
        // 참고: 여는 순간 메인 버튼을 끄는 건 GameManager.OnClickSettings()에서 이미 처리함
    }

    // ★ [수정됨] 닫을 때 메인 메뉴 복구 로직 추가
    public void CloseSettingPanel()
    {
        // 1. 설정 창 끄기
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        // 2. ★ GameManager에게 메인 버튼 그룹을 켜달라고 요청
        // (주의: GameManager.Instance.OnCloseSettings()를 부르면 무한 루프가 될 수 있으므로 직접 버튼만 켭니다)
        if (GameManager.Instance != null && GameManager.Instance.mainButtonGroup != null)
        {
            GameManager.Instance.mainButtonGroup.SetActive(true);
        }
    }
}