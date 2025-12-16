using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameTimeManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("현실 시간 몇 초가 게임의 하루(24시간)인가요?")]
    public float dayDurationInSeconds = 120f; // 기본 2분 (120초) = 24시간

    [Tooltip("게임 시작 시 시간 (0 ~ 24)")]
    [Range(0, 24)]
    public float startHour = 12f; // 12시 시작

    // 현재 시간 (외부에서 읽기만 가능하게 프로퍼티로 변경 추천하지만, 인스펙터 확인용으로 public 유지)
    public float currentTime;

    [Header("UI References")]
    public TMP_Text timeText;      // 시간 표시 텍스트
    public Image dayNightIcon;     // 해/달 아이콘 이미지

    [Header("Icons")]
    public Sprite sunSprite;       // 해 이미지
    public Sprite moonSprite;      // 달 이미지

    // 시작 시간을 초 단위로 변환해둘 변수
    private float startOffset;

    void Start()
    {
        // 예: 12시에 시작하려면, 하루 길이의 50%만큼 미리 시간이 흐른 것으로 처리
        // (12 / 24) * 120초 = 60초를 오프셋으로 설정
        startOffset = (startHour / 24f) * dayDurationInSeconds;

        UpdateGameTime();
        UpdateUI();
    }

    void Update()
    {
        UpdateGameTime();
        UpdateUI();
    }

    void UpdateGameTime()
    {
        // ★ [핵심] Time.time을 이용한 시간 계산
        // 1. (현재까지 흐른 시간 + 시작 오프셋)을 구함
        float totalSeconds = Time.time + startOffset;

        // 2. 모듈러 연산(%)으로 현재 사이클의 시간(초)을 구함
        // 예: 130초가 지났고 하루가 120초면, 나머지는 10초
        float currentCycleSeconds = totalSeconds % dayDurationInSeconds;

        // 3. 이를 0~24시 비율로 변환
        // (현재초 / 하루총초) * 24
        currentTime = (currentCycleSeconds / dayDurationInSeconds) * 24f;
    }

    void UpdateUI()
    {
        // 1. 시간 텍스트 갱신 (00:00 형식)
        int hour = Mathf.FloorToInt(currentTime);
        int minute = Mathf.FloorToInt((currentTime - hour) * 60f);

        if (timeText != null)
        {
            timeText.text = string.Format("{0:00}:{1:00}", hour, minute);
        }

        // 2. 해/달 아이콘 변경 (6시 ~ 19시: 낮)
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