using UnityEngine;

public class NeckTracker : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("회전시킬 뼈(Neck 또는 Head)의 Transform")]
    [SerializeField] private Transform _targetBone;

    [Tooltip("메인 메뉴 카메라 (비워둘 경우 MainCamera)")]
    [SerializeField] private Camera _menuCamera;

    [Header("Axis Enable Settings")]
    [Tooltip("체크 해제 시 상하(Pitch/X축) 회전을 하지 않습니다.")]
    [SerializeField] private bool _enablePitch = true; // 목(Neck)은 이것만 켜면 됨

    [Tooltip("체크 해제 시 좌우(Yaw/Y축) 회전을 하지 않습니다.")]
    [SerializeField] private bool _enableYaw = true;   // 목(Neck)은 이걸 끄면 됨

    [Header("Rotation Limits")]
    [Tooltip("위/아래(Pitch) 최대 회전 각도")]
    [Range(0f, 90f)]
    [SerializeField] private float _maxPitchAngle = 30f;

    [Tooltip("좌/우(Yaw) 최대 회전 각도")]
    [Range(0f, 180f)]
    [SerializeField] private float _maxYawAngle = 60f;

    [Tooltip("회전 부드러움 정도")]
    [SerializeField] private float _smoothTime = 10f;

    [Header("Invert Settings")]
    [SerializeField] private bool _invertPitch = false;
    [SerializeField] private bool _invertYaw = false;

    // 내부 변수
    private Quaternion _initialRotation;

    private void Start()
    {
        InitializeSetup();
    }

    private void LateUpdate()
    {
        if (_targetBone == null || _menuCamera == null) return;

        RotateBoneTowardsMouse();
    }

    private void InitializeSetup()
    {
        if (_menuCamera == null) _menuCamera = Camera.main;
        if (_targetBone != null) _initialRotation = _targetBone.localRotation;
    }

    private void RotateBoneTowardsMouse()
    {
        // 1. 마우스 위치 정규화
        Vector3 mousePos = Input.mousePosition;
        float xPercent = Mathf.Clamp((mousePos.x / Screen.width) - 0.5f, -0.5f, 0.5f);
        float yPercent = Mathf.Clamp((mousePos.y / Screen.height) - 0.5f, -0.5f, 0.5f);

        // 2. 각도 계산 (활성화된 축만 계산)
        float pitchAmount = 0f;
        float yawAmount = 0f;

        // Pitch (X축 회전) - 마우스 Y 움직임에 반응
        if (_enablePitch)
        {
            float rawPitch = yPercent * 2f * _maxPitchAngle;
            pitchAmount = _invertPitch ? rawPitch : -rawPitch;
            pitchAmount = Mathf.Clamp(pitchAmount, -_maxPitchAngle, _maxPitchAngle);
        }

        // Yaw (Y축 회전) - 마우스 X 움직임에 반응
        if (_enableYaw)
        {
            float rawYaw = xPercent * 2f * _maxYawAngle;
            yawAmount = _invertYaw ? -rawYaw : rawYaw;
            yawAmount = Mathf.Clamp(yawAmount, -_maxYawAngle, _maxYawAngle);
        }

        // 3. 목표 회전값 생성 (Pitch는 X축, Yaw는 Y축)
        Vector3 targetEuler = new Vector3(pitchAmount, yawAmount, 0f);

        // 4. 초기 회전값에 더하기
        Quaternion targetRotation = _initialRotation * Quaternion.Euler(targetEuler);

        // 5. 적용
        _targetBone.localRotation = Quaternion.Slerp(_targetBone.localRotation, targetRotation, Time.deltaTime * _smoothTime);
    }
}