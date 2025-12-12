using UnityEngine;
using UnityEngine.UI;

public class DynamicCrosshair : MonoBehaviour
{
    // === Inspector에서 연결할 UI RectTransform 요소들 ===
    public RectTransform topArm;
    public RectTransform bottomArm;
    public RectTransform leftArm;
    public RectTransform rightArm;

    [Header("UI Elements")]
    public GameObject centerDot;

    private RectTransform crosshairContainer;

    // === 회전 설정 변수 ===
    [Header("Rotation Settings")]
    public float rotationSpeed = 10f;
    private Quaternion targetRotation;

    // === 스프레드 설정 변수 (사격/일반 상태) ===
    [Header("General Spread Settings")]
    public float defaultSpread = 80f;
    public float maxSpread = 150f;
    public float spreadAmount = 20f;   // 일반 상태 사격 시 벌어지는 양
    public float recoverySpeed = 5f;

    // === Aim Down Sight (ADS) 설정 변수 ===
    [Header("Aim Down Sight (ADS) Settings")]
    public float adsSpread = 20f;
    public float adsRecoverySpeed = 15f;
    // 💡 추가됨: ADS 상태에서 사격 시 벌어지는 양
    public float adsFireSpreadAmount = 5f;

    private float currentSpread;

    // === 카메라 설정 변수 (추가) ===
    [Header("Camera Zoom Settings")]
    public Camera playerCamera; // Inspector에서 메인 카메라 연결
    public float zoomFOV = 30f; // ADS 상태에서의 FOV (더 작은 값이 확대됨)
    public float defaultFOV = 60f; // 일반 상태에서의 기본 FOV
    public float zoomSpeed = 8f; // 줌 전환 속도

    void Awake()
    {
        crosshairContainer = GetComponent<RectTransform>();
    }

    void Start()
    {
        currentSpread = defaultSpread;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;

        targetRotation = Quaternion.Euler(0, 0, 0);
        UpdateCrosshairPosition(currentSpread);

        if (centerDot != null)
        {
            centerDot.SetActive(false);
        }
    }

    void Update()
    {
        // 1. 마우스 위치 추적
        if (crosshairContainer != null)
        {
            crosshairContainer.position = Input.mousePosition;
        }

        // 2. 목표 상태 설정 (회전, 목표 Spread, 회복 속도)
        float targetSpread;
        float currentRecoverySpeed;

        // 💡 추가: 목표 FOV 변수
        float targetFOV;

        // 마우스 우클릭 (ADS 상태)
        if (Input.GetMouseButton(1))
        {
            targetRotation = Quaternion.Euler(0, 0, -90f);
            targetSpread = adsSpread;
            currentRecoverySpeed = adsRecoverySpeed;

            // ADS 상태의 목표 FOV
            targetFOV = zoomFOV;

            // Center Dot 활성화
            if (centerDot != null && !centerDot.activeSelf)
            {
                centerDot.SetActive(true);
            }
        }
        else // 일반 상태
        {
            targetRotation = Quaternion.Euler(0, 0, 0f);
            targetSpread = defaultSpread;
            currentRecoverySpeed = recoverySpeed;

            // 일반 상태의 목표 FOV
            targetFOV = defaultFOV;

            // Center Dot 비활성화
            if (centerDot != null && centerDot.activeSelf)
            {
                centerDot.SetActive(false);
            }
        }

        // 3. 좌클릭 시 벌어지는 로직 (ADS 상태와 관계없이 적용)
        if (Input.GetMouseButtonDown(0))
        {
            float fireSpread = spreadAmount; // 기본적으로 일반 탄퍼짐 사용

            // 💡 우클릭 중이면 (ADS 상태이면) ADS용 작은 탄퍼짐 값 적용
            if (Input.GetMouseButton(1))
            {
                fireSpread = adsFireSpreadAmount;
            }

            currentSpread += fireSpread;
            // 최대 벌어짐 값보다 커지지 않도록 제한
            currentSpread = Mathf.Min(currentSpread, maxSpread);
        }

        // 4. 회전 적용 (부드러운 전환)
        if (crosshairContainer != null)
        {
            crosshairContainer.rotation = Quaternion.Slerp(
                crosshairContainer.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }

        // 5. Spread 적용 (탄퍼짐 복구)
        currentSpread = Mathf.Lerp(currentSpread, targetSpread, Time.deltaTime * currentRecoverySpeed);

        // FOV 적용 (카메라 줌인/줌아웃)
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = Mathf.Lerp(
                playerCamera.fieldOfView,
                targetFOV,
                Time.deltaTime * zoomSpeed
            );
        }

        // 6. UI 적용
        UpdateCrosshairPosition(currentSpread);
    }

    // (UpdateCrosshairPosition 함수는 이전과 동일합니다.)
    void UpdateCrosshairPosition(float spread)
    {
        if (topArm != null)
            topArm.anchoredPosition = new Vector2(0, spread);

        if (bottomArm != null)
            bottomArm.anchoredPosition = new Vector2(0, -spread);

        if (leftArm != null)
            leftArm.anchoredPosition = new Vector2(-spread, 0);

        if (rightArm != null)
            rightArm.anchoredPosition = new Vector2(spread, 0);
    }
}