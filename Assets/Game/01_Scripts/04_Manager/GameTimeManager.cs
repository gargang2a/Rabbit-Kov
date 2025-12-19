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
    [Range(0, 360)]
    public float sunDirectionY = 90f;

    [Header("Lighting (Ambient)")]
    public Gradient ambientSkyColor;
    public Gradient ambientEquatorColor;
    public Gradient ambientGroundColor;

    [Header("UI References")]
    public TMP_Text timeText;

    // 기존 아이콘 교체 방식 (원한다면 유지, 필요 없으면 제거 가능)
    public Image dayNightIcon;
    public Sprite sunSprite;
    public Sprite moonSprite;

    // ★ [NEW] 회전하는 UI 설정
    [Header("UI - Rotating Dial (Compass)")]
    [Tooltip("해와 달이 자식으로 있는 부모 Pivot 객체를 넣으세요")]
    public RectTransform celestialDialPivot;

    [Tooltip("12시(정오)에 해가 정확히 위에 오도록 각도를 보정합니다. (기본값 180 추천)")]
    public float dialRotationOffset = 180f;

    private float startOffset;

    void Start()
    {
        startOffset = (startHour / 24f) * dayDurationInSeconds;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        UpdateGameTime();
        UpdateUI();
    }

    void Update()
    {
        UpdateGameTime();
        UpdateUI();
        UpdateLightRotation();
        UpdateAmbientLight();
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
            sunLight.transform.rotation = Quaternion.Euler(rotX, sunDirectionY, 0f);

            if (currentTime >= 6f && currentTime <= 18f)
                sunLight.intensity = 1.0f;
            else
                sunLight.intensity = 0.0f;
        }
    }

    void UpdateAmbientLight()
    {
        float timePercent = currentTime / 24f;
        RenderSettings.ambientSkyColor = ambientSkyColor.Evaluate(timePercent);
        RenderSettings.ambientEquatorColor = ambientEquatorColor.Evaluate(timePercent);
        RenderSettings.ambientGroundColor = ambientGroundColor.Evaluate(timePercent);
    }

    void UpdateUI()
    {
        // 1. 텍스트 시간 표시
        int hour = Mathf.FloorToInt(currentTime);
        int minute = Mathf.FloorToInt((currentTime - hour) * 60f);

        if (timeText != null)
            timeText.text = string.Format("{0:00}:{1:00}", hour, minute);

        // 2. 기존 아이콘 교체 로직 (유지)
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

        // ★ [NEW] 3. 나침반 위 해/달 회전 로직
        if (celestialDialPivot != null)
        {
            // 하루 24시간 = 360도
            // Z축을 기준으로 시계방향(-)으로 회전해야 함
            float zRot = -(currentTime / 24f) * 360f;

            // 오프셋 적용 (12시에 해가 맨 위로 오게 맞추기 위함)
            celestialDialPivot.localRotation = Quaternion.Euler(0f, 0f, zRot + dialRotationOffset);
        }
    }
}