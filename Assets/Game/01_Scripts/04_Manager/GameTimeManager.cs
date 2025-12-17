using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameTimeManager : MonoBehaviour
{
    [Header("Settings")]
    public float dayDurationInSeconds = 120f;
    [Range(0, 24)]
    public float startHour = 12f;
    public float currentTime;

    [Header("Lighting (Sun)")]
    public Light sunLight;

    [Header("Lighting (Ambient) - ★ 코드로 제어")]
    // 인스펙터에서 시간대별 색상을 지정할 수 있는 그라데이션 바
    public Gradient ambientSkyColor;     // 하늘색
    public Gradient ambientEquatorColor; // 지평선색
    public Gradient ambientGroundColor;  // 바닥색

    [Header("UI References")]
    public TMP_Text timeText;
    public Image dayNightIcon;

    [Header("Icons")]
    public Sprite sunSprite;
    public Sprite moonSprite;

    private float startOffset;

    void Start()
    {
        startOffset = (startHour / 24f) * dayDurationInSeconds;

        // ★ [핵심] 게임 시작 시 환경광 모드를 'Gradient'로 강제 설정
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;

        UpdateGameTime();
        UpdateUI();
    }

    void Update()
    {
        UpdateGameTime();
        UpdateUI();
        UpdateLightRotation();
        UpdateAmbientLight(); // ★ 환경광 업데이트 추가
    }

    void UpdateGameTime()
    {
        float totalSeconds = Time.time + startOffset;
        float currentCycleSeconds = totalSeconds % dayDurationInSeconds;
        currentTime = (currentCycleSeconds / dayDurationInSeconds) * 24f;
    }

    void UpdateLightRotation()
    {
        if (sunLight != null)
        {
            float rotX = (currentTime / 24f) * 360f - 90f;
            sunLight.transform.rotation = Quaternion.Euler(rotX, -170f, 0f);

            // (선택) 밤에는 해(Directional Light) 자체를 꺼버리거나 어둡게 하기
            // 해가 지면(18시~6시) 빛 강도를 0으로, 뜨면 1로
            if (currentTime >= 6f && currentTime <= 18f)
                sunLight.intensity = 1.0f;
            else
                sunLight.intensity = 0.0f; // 밤에는 달빛(Ambient)만 남김
        }
    }

    // ★ [추가] 시간에 따라 환경광 색상 변경
    void UpdateAmbientLight()
    {
        // 현재 시간 비율 (0.0 ~ 1.0)
        float timePercent = currentTime / 24f;

        // 그라데이션에서 현재 시간에 맞는 색을 뽑아옴
        RenderSettings.ambientSkyColor = ambientSkyColor.Evaluate(timePercent);
        RenderSettings.ambientEquatorColor = ambientEquatorColor.Evaluate(timePercent);
        RenderSettings.ambientGroundColor = ambientGroundColor.Evaluate(timePercent);
    }

    void UpdateUI()
    {
        int hour = Mathf.FloorToInt(currentTime);
        int minute = Mathf.FloorToInt((currentTime - hour) * 60f);

        if (timeText != null)
            timeText.text = string.Format("{0:00}:{1:00}", hour, minute);

        if (dayNightIcon != null)
        {
            if (currentTime >= 6f && currentTime < 19f)
            {
                if (sunSprite != null) dayNightIcon.sprite = sunSprite;
            }
            else
            {
                if (moonSprite != null) dayNightIcon.sprite = moonSprite;
            }
        }
    }
}