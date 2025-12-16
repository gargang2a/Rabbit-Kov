using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Coroutine 사용을 위해 필수

public class DynamicCrosshair : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform _topArm;
    [SerializeField] private RectTransform _bottomArm;
    [SerializeField] private RectTransform _leftArm;
    [SerializeField] private RectTransform _rightArm;
    [SerializeField] private GameObject _centerDot;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("General Spread Settings")]
    [SerializeField] private float _defaultSpread = 40f;
    [SerializeField] private float _maxSpread = 150f;
    [SerializeField] private float _spreadAmount = 20f;   // 일반 사격 시 벌어짐
    [SerializeField] private float _recoverySpeed = 5f;

    [Header("Aim Down Sight (ADS) Settings")]
    [SerializeField] private float _adsSpread = 20f;
    [SerializeField] private float _adsRecoverySpeed = 15f;
    [SerializeField] private float _adsFireSpreadAmount = 5f; // ADS 사격 시 벌어짐

    [Header("Camera Zoom Settings")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private float _zoomFOV = 55f;
    [SerializeField] private float _defaultFOV = 60f;
    [SerializeField] private float _zoomSpeed = 8f;

    // 내부 변수
    private RectTransform _crosshairContainer;
    private Quaternion _targetRotation;
    private float _currentSpread;

    void Awake()
    {
        _crosshairContainer = GetComponent<RectTransform>();

        // [핵심 수정 1] 크로스헤어 UI가 마우스 클릭을 가로채지 못하게 강제 설정
        // 자식에 있는 모든 Image 컴포넌트를 찾아서 Raycast Target을 끕니다.
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

        // [핵심 수정 2] 시작 시 커서 설정을 1프레임 뒤로 미룸 (초기화 씹힘 방지)
        StartCoroutine(InitializeCursorState());
    }

    /// <summary>
    /// 게임 시작 직후 혹은 포커스가 돌아왔을 때 커서 상태를 재설정
    /// </summary>
    private IEnumerator InitializeCursorState()
    {
        yield return null; // 1프레임 대기
        SetCursorState();
    }

    /// <summary>
    /// 알트탭 등으로 창을 나갔다가 돌아왔을 때 커서 상태 복구
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            SetCursorState();
        }
    }

    private void SetCursorState()
    {
        // 커서를 숨김
        Cursor.visible = false;

        // [핵심 수정 3] None 대신 Confined 사용
        // Confined: 마우스가 게임 창 밖으로 나가지 못하게 가둠 -> 클릭 정확도 상승
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        // 1. 마우스 위치 추적
        if (_crosshairContainer != null)
        {
            _crosshairContainer.position = Input.mousePosition;
        }

        // 2. 목표 상태 설정 (ADS 여부에 따른 분기)
        float targetSpread;
        float currentRecoverySpeed;
        float targetFOV;

        // 우클릭 (ADS 상태)
        if (Input.GetMouseButton(1))
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

        // 3. 좌클릭 (사격) 시 벌어짐 처리
        if (Input.GetMouseButtonDown(0))
        {
            float fireSpread = Input.GetMouseButton(1) ? _adsFireSpreadAmount : _spreadAmount;

            _currentSpread += fireSpread;
            _currentSpread = Mathf.Min(_currentSpread, _maxSpread);
        }

        // 4. 회전 적용 (Slerp)
        if (_crosshairContainer != null)
        {
            _crosshairContainer.rotation = Quaternion.Slerp(
                _crosshairContainer.rotation,
                _targetRotation,
                Time.deltaTime * _rotationSpeed
            );
        }

        // 5. Spread 복구 (Lerp)
        _currentSpread = Mathf.Lerp(_currentSpread, targetSpread, Time.deltaTime * currentRecoverySpeed);

        // 6. 카메라 줌 (FOV)
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

        // [개발용] ESC 누르면 커서 보이게 하기 (테스트 편의성)
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
}