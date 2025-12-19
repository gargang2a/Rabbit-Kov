using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    // ★ [New] 외부에서 호출하기 위한 싱글톤
    public static QuarterViewCamera Instance { get; private set; }

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
    [Range(0f, 1f)] public float baseShiftRatio = 0.15f;

    [Tooltip("우클릭 시 마우스 쪽으로 카메라가 이동하는 비율 (0.4 ~ 0.6 추천)")]
    [Range(0f, 1f)] public float aimShiftRatio = 0.5f;

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
    private Vector3 _currentShift;
    private Vector3 _smoothTargetPos;

    // ★ [New] 흔들림 관련 변수
    private float _shakeTimer;
    private float _shakeMagnitude;
    private Vector3 _currentShakePos;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

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

        // 2. 입력 및 줌 처리
        bool isAiming = Input.GetMouseButton(1);
        float targetFov = isAiming ? zoomedFov : _defaultFov;
        float currentRatio = isAiming ? aimShiftRatio : baseShiftRatio;

        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, Time.deltaTime * zoomSpeed);

        // 3. 마우스 쉬프트(Pan) 계산
        Vector3 mousePos = GetMouseGroundPos();
        Vector3 dir = mousePos - target.position;
        Vector3 clampedDir = Vector3.ClampMagnitude(dir, maxShiftDistance);
        Vector3 targetShift = clampedDir * currentRatio;
        targetShift.y = 0;

        _currentShift = Vector3.Lerp(_currentShift, targetShift, Time.deltaTime * shiftSpeed);


        // 4. ★ [New] 흔들림(Shake) 계산
        if (_shakeTimer > 0)
        {
            // 구체 범위 내에서 랜덤 떨림
            _currentShakePos = Random.insideUnitSphere * _shakeMagnitude;
            _shakeTimer -= Time.deltaTime;
        }
        else
        {
            // 떨림 종료 시 부드럽게 원위치
            _currentShakePos = Vector3.MoveTowards(_currentShakePos, Vector3.zero, Time.deltaTime * 5f);
        }


        // 5. 최종 위치 적용 (플레이어 + 마우스이동 + 흔들림 + 각도오프셋)
        Vector3 finalPos = (_smoothTargetPos + _currentShift) + _currentShakePos + _staticOffset;

        transform.position = finalPos;
        transform.rotation = Quaternion.Euler(xAngle, yAngle, 0);
    }

    /// <summary>
    /// 외부에서 카메라 흔들기 요청
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        _shakeTimer = duration;
        _shakeMagnitude = magnitude;
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