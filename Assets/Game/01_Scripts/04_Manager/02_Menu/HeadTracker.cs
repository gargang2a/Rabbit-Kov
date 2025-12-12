using UnityEngine;

public class HeadTracker : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("회전시킬 머리 뼈의 Transform을 할당하세요.")]
    [SerializeField] private Transform _headBone;

    [Tooltip("메인 메뉴를 비추는 카메라 (비워둘 경우 MainCamera 사용)")]
    [SerializeField] private Camera _menuCamera;

    [Header("Rotation Limits")]
    [Tooltip("위/아래(Pitch) 최대 회전 각도")]
    [Range(0f, 90f)]
    [SerializeField] private float _maxPitchAngle = 30f; // 보통 30~45도 추천

    [Tooltip("좌/우(Yaw) 최대 회전 각도")]
    [Range(0f, 180f)]
    [SerializeField] private float _maxYawAngle = 60f;   // 보통 60~80도 추천

    [Tooltip("회전 부드러움 정도 (값이 클수록 느리게 따라감)")]
    [SerializeField] private float _smoothTime = 10f;

    [Header("Invert Settings")]
    [Tooltip("체크 시 상하(Pitch) 회전 방향 반전")]
    [SerializeField] private bool _invertPitch = false;

    [Tooltip("체크 시 좌우(Yaw) 회전 방향 반전")]
    [SerializeField] private bool _invertYaw = false;

    // 내부 상태 변수
    private Quaternion _initialRotation;

    private void Start()
    {
        InitializeSetup();
    }

    private void LateUpdate()
    {
        if (_headBone == null || _menuCamera == null) return;

        RotateHeadTowardsMouse();
    }

    private void InitializeSetup()
    {
        if (_menuCamera == null)
        {
            _menuCamera = Camera.main;
        }

        if (_headBone != null)
        {
            _initialRotation = _headBone.localRotation;
        }
    }

    private void RotateHeadTowardsMouse()
    {
        // 1. 마우스 위치 정규화 (-0.5 ~ 0.5)
        Vector3 mousePos = Input.mousePosition;

        // 화면 밖으로 마우스가 나갔을 때 값이 튀지 않도록 Clamp 처리
        float xPercent = Mathf.Clamp((mousePos.x / Screen.width) - 0.5f, -0.5f, 0.5f);
        float yPercent = Mathf.Clamp((mousePos.y / Screen.height) - 0.5f, -0.5f, 0.5f);

        // 2. 각 축별 최대 각도 적용
        // xPercent * 2f는 -1.0 ~ 1.0 범위가 됨
        float yawAmount = xPercent * 2f * _maxYawAngle;     // 좌우 제한값 적용
        float pitchAmount = yPercent * 2f * _maxPitchAngle; // 상하 제한값 적용

        // 3. 방향 반전 처리
        float finalPitch = _invertPitch ? pitchAmount : -pitchAmount;
        float finalYaw = _invertYaw ? -yawAmount : yawAmount;

        // 4. 최종 각도 클램핑 (안전장치: 설정한 최대 각도를 절대 넘지 않도록 강제)
        finalPitch = Mathf.Clamp(finalPitch, -_maxPitchAngle, _maxPitchAngle);
        finalYaw = Mathf.Clamp(finalYaw, -_maxYawAngle, _maxYawAngle);

        // 5. 목표 회전값 생성
        Vector3 targetEuler = new Vector3(finalPitch, finalYaw, 0f);

        // 6. 초기 회전값에 더하기
        Quaternion targetRotation = _initialRotation * Quaternion.Euler(targetEuler);

        // 7. 부드러운 회전 적용
        _headBone.localRotation = Quaternion.Slerp(_headBone.localRotation, targetRotation, Time.deltaTime * _smoothTime);
    }
}