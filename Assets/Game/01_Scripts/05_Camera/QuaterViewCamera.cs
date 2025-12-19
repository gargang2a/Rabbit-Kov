using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          // 플레이어

    [Header("Base Settings")]
    public float distance = 40f;      // 카메라 거리
    public float smoothSpeed = 10f;   // 플레이어 추적 속도

    [Header("Angle Settings (Fixed)")]
    [Range(0f, 90f)] public float xAngle = 55f;  // 수직 기울기
    [Range(0f, 360f)] public float yAngle = 45f; // 수평 회전

    [Header("Mouse Shift Settings")]
    [Tooltip("평소에 마우스 쪽으로 카메라가 이동하는 비율 (0.1 ~ 0.2 추천)")]
    [Range(0f, 1f)] public float baseShiftRatio = 0.15f; // ★ 평소 움직임

    [Tooltip("우클릭 시 마우스 쪽으로 카메라가 이동하는 비율 (0.4 ~ 0.6 추천)")]
    [Range(0f, 1f)] public float aimShiftRatio = 0.5f;   // ★ 조준 시 움직임

    [Tooltip("마우스 쪽으로 이동할 수 있는 최대 거리 제한")]
    public float maxShiftDistance = 15f;

    [Tooltip("화면 이동 반응 속도")]
    public float shiftSpeed = 5f;

    [Header("Zoom Settings")]
    [Tooltip("우클릭 시 적용될 FOV")]
    public float zoomedFov = 40f;
    public float zoomSpeed = 10f;

    // 내부 변수
    private Camera _cam;
    private float _defaultFov;
    private Vector3 _staticOffset;
    private Vector3 _currentShift;    // 현재 카메라 이동량
    private Vector3 _smoothTargetPos; // 플레이어 위치 스무딩용

    void Start()
    {
        _cam = GetComponent<Camera>();
        _defaultFov = _cam.fieldOfView;
        _smoothTargetPos = transform.position;

        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        CalculateStaticOffset();

        // 초기화
        if (target != null)
        {
            _smoothTargetPos = target.position;
            transform.position = target.position + _staticOffset;
            transform.rotation = Quaternion.Euler(xAngle, yAngle, 0);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 플레이어 위치 부드럽게 따라가기
        _smoothTargetPos = Vector3.Lerp(_smoothTargetPos, target.position, Time.deltaTime * smoothSpeed);

        // 2. 입력 상태 확인
        bool isAiming = Input.GetMouseButton(1);

        // 3. 목표 FOV 및 Shift 비율 설정
        float targetFov = isAiming ? zoomedFov : _defaultFov;
        float currentRatio = isAiming ? aimShiftRatio : baseShiftRatio; // ★ 여기서 비율 결정

        // FOV 적용
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, Time.deltaTime * zoomSpeed);

        // 4. 마우스 쉬프트(Pan) 계산
        Vector3 mousePos = GetMouseGroundPos();
        Vector3 dir = mousePos - target.position; // 플레이어 -> 마우스 벡터

        // 최대 거리 제한 (Clamp)
        Vector3 clampedDir = Vector3.ClampMagnitude(dir, maxShiftDistance);

        // 비율 적용 (평소엔 baseRatio, 조준땐 aimRatio가 적용됨)
        Vector3 targetShift = clampedDir * currentRatio;
        targetShift.y = 0; // 높이는 변하지 않음

        // 5. 쉬프트 값 부드럽게 갱신 (Lerp)
        _currentShift = Vector3.Lerp(_currentShift, targetShift, Time.deltaTime * shiftSpeed);

        // 6. 최종 위치 적용
        Vector3 finalPos = (_smoothTargetPos + _currentShift) + _staticOffset;

        transform.position = finalPos;
        transform.rotation = Quaternion.Euler(xAngle, yAngle, 0); // 회전 고정
    }

    private Vector3 GetMouseGroundPos()
    {
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, new Vector3(0, target.position.y, 0));

        if (ground.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return target.position;
    }

    private void CalculateStaticOffset()
    {
        Quaternion rot = Quaternion.Euler(xAngle, yAngle, 0);
        _staticOffset = rot * Vector3.back * distance;
    }

    private void OnValidate()
    {
        if (Application.isPlaying && target != null)
        {
            CalculateStaticOffset();
            transform.rotation = Quaternion.Euler(xAngle, yAngle, 0);
        }
    }
}