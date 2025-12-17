using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DynamicCrosshair : MonoBehaviour
{
    // 싱글톤 패턴 (어디서든 접근 가능하게)
    public static DynamicCrosshair Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RectTransform _topArm;
    [SerializeField] private RectTransform _bottomArm;
    [SerializeField] private RectTransform _leftArm;
    [SerializeField] private RectTransform _rightArm;
    [SerializeField] private GameObject _centerDot;

    [Header("Custom Cursor Settings")]
    [SerializeField] private Texture2D _customCursorTexture; // ★ 커스텀 커서 이미지
    [SerializeField] private Vector2 _cursorHotspot = Vector2.zero; // 커서 클릭 지점 (보통 0,0)

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("General Spread Settings")]
    [SerializeField] private float _defaultSpread = 40f;
    [SerializeField] private float _maxSpread = 150f;
    [SerializeField] private float _spreadAmount = 20f;
    [SerializeField] private float _recoverySpeed = 5f;

    [Header("Aim Down Sight (ADS) Settings")]
    [SerializeField] private float _adsSpread = 20f;
    [SerializeField] private float _adsRecoverySpeed = 15f;
    [SerializeField] private float _adsFireSpreadAmount = 5f;

    [Header("Camera Zoom Settings")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private float _zoomFOV = 55f;
    [SerializeField] private float _defaultFOV = 60f;
    [SerializeField] private float _zoomSpeed = 8f;

    // 내부 변수
    private RectTransform _crosshairContainer;
    private Quaternion _targetRotation;
    private float _currentSpread;
    private bool _isHoveringUI = false; // 현재 UI 위에 있는지 여부

    void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _crosshairContainer = GetComponent<RectTransform>();

        // 크로스헤어 UI가 마우스 클릭을 가로채지 못하게 설정
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            img.raycastTarget = false;
        }
    }

    void Start()
    {
        _currentSpread = _defaultSpread;
        _targetRotation = Quaternion.Euler(0, 0, 0);

        if (_centerDot != null)
        {
            _centerDot.SetActive(false);
        }

        UpdateCrosshairPosition(_currentSpread);
        StartCoroutine(InitializeCursorState());
    }

    private IEnumerator InitializeCursorState()
    {
        yield return null;
        SetGameplayCursorState(); // 게임 시작 시엔 게임플레이 모드
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            // 포커스가 돌아왔을 때 현재 상태에 맞춰 커서 복구
            if (_isHoveringUI) SetUICursorState();
            else SetGameplayCursorState();
        }
    }

    void Update()
    {
        // ★ UI 위에 있을 때는 크로스헤어 로직 중단 (커서만 보여줌)
        if (_isHoveringUI)
        {
            // 혹시라도 커서가 꺼져있다면 다시 켬
            if (Cursor.visible == false) SetUICursorState();
            return;
        }

        // --- 이하 게임플레이(크로스헤어) 로직 ---

        // 1. 마우스 위치 추적
        if (_crosshairContainer != null)
        {
            _crosshairContainer.position = Input.mousePosition;
        }

        // 2. 목표 상태 설정 (ADS 여부)
        float targetSpread;
        float currentRecoverySpeed;
        float targetFOV;

        if (Input.GetMouseButton(1)) // 우클릭 (ADS)
        {
            _targetRotation = Quaternion.Euler(0, 0, -90f);
            targetSpread = _adsSpread;
            currentRecoverySpeed = _adsRecoverySpeed;
            targetFOV = _zoomFOV;

            if (_centerDot != null && !_centerDot.activeSelf)
                _centerDot.SetActive(true);
        }
        else // 일반 상태
        {
            _targetRotation = Quaternion.Euler(0, 0, 0f);
            targetSpread = _defaultSpread;
            currentRecoverySpeed = _recoverySpeed;
            targetFOV = _defaultFOV;

            if (_centerDot != null && _centerDot.activeSelf)
                _centerDot.SetActive(false);
        }

        // 3. 사격 시 벌어짐
        if (Input.GetMouseButtonDown(0))
        {
            float fireSpread = Input.GetMouseButton(1) ? _adsFireSpreadAmount : _spreadAmount;
            _currentSpread += fireSpread;
            _currentSpread = Mathf.Min(_currentSpread, _maxSpread);
        }

        // 4. 회전 적용
        if (_crosshairContainer != null)
        {
            _crosshairContainer.rotation = Quaternion.Slerp(
                _crosshairContainer.rotation,
                _targetRotation,
                Time.deltaTime * _rotationSpeed
            );
        }

        // 5. Spread 복구
        _currentSpread = Mathf.Lerp(_currentSpread, targetSpread, Time.deltaTime * currentRecoverySpeed);

        // 6. 카메라 줌
        if (_playerCamera != null)
        {
            _playerCamera.fieldOfView = Mathf.Lerp(
                _playerCamera.fieldOfView,
                targetFOV,
                Time.deltaTime * _zoomSpeed
            );
        }

        // 7. UI 위치 갱신
        UpdateCrosshairPosition(_currentSpread);

        // [개발용] ESC
        if (Application.isEditor && Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void UpdateCrosshairPosition(float spread)
    {
        if (_topArm != null) _topArm.anchoredPosition = new Vector2(0, spread);
        if (_bottomArm != null) _bottomArm.anchoredPosition = new Vector2(0, -spread);
        if (_leftArm != null) _leftArm.anchoredPosition = new Vector2(-spread, 0);
        if (_rightArm != null) _rightArm.anchoredPosition = new Vector2(spread, 0);
    }

    // =========================================================
    // ★ 커서 상태 관리 함수들
    // =========================================================

    // 1. 게임플레이 모드 (크로스헤어 ON, 커서 OFF)
    private void SetGameplayCursorState()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined; // 창 밖으로 못 나가게
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // 기본 커서로 초기화 (안보이지만)

        ToggleCrosshairVisuals(true); // 크로스헤어 보이기
    }

    // 2. UI 모드 (크로스헤어 OFF, 커스텀 커서 ON)
    private void SetUICursorState()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None; // 자유롭게 이동

        // ★ 커스텀 커서 적용
        if (_customCursorTexture != null)
        {
            Cursor.SetCursor(_customCursorTexture, _cursorHotspot, CursorMode.ForceSoftware);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // 없으면 기본 화살표
        }

        ToggleCrosshairVisuals(false); // 크로스헤어 숨기기
    }

    // 크로스헤어 이미지 켜고 끄기
    private void ToggleCrosshairVisuals(bool isActive)
    {
        if (_topArm != null) _topArm.gameObject.SetActive(isActive);
        if (_bottomArm != null) _bottomArm.gameObject.SetActive(isActive);
        if (_leftArm != null) _leftArm.gameObject.SetActive(isActive);
        if (_rightArm != null) _rightArm.gameObject.SetActive(isActive);
        if (_centerDot != null) _centerDot.SetActive(isActive);
    }

    // ★ 외부(UI)에서 호출할 함수
    public void SetUIHoverState(bool isHovering)
    {
        if (_isHoveringUI == isHovering) return; // 상태가 같으면 무시

        _isHoveringUI = isHovering;

        if (_isHoveringUI) SetUICursorState();
        else SetGameplayCursorState();
    }
}