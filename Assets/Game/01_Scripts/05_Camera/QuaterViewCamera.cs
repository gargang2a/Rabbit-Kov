using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    public static QuarterViewCamera Instance { get; private set; }

    [Header("Target")]
    public Transform target;

    [Header("Base Settings")]
    public float distance = 25f;
    public float smoothSpeed = 10f; // 이동(X, Z) 따라가는 속도

    [Tooltip("높이(Y) 따라가는 속도 (낮을수록 위아래 떨림이 사라짐)")]
    [SerializeField] private float _heightSmoothSpeed = 2.0f; // ★ [New] 높이 전용

    [Header("Angle Settings (Fixed)")]
    [Range(0f, 90f)] public float xAngle = 55f;
    [Range(0f, 360f)] public float yAngle = 45f;

    [Header("Mouse Shift Settings")]
    [Range(0f, 1f)] public float baseShiftRatio = 0.15f;
    [Range(0f, 1f)] public float aimShiftRatio = 0.5f;
    public float maxShiftDistance = 15f;
    public float shiftSpeed = 5f;

    [Header("Zoom Settings")]
    public float zoomedFov = 40f;
    public float zoomSpeed = 10f;

    // 내부 변수
    private Camera _cam;
    private float _defaultFov;
    private Vector3 _staticOffset;
    private Vector3 _currentShift;

    // ★ [New] 떨림 방지를 위한 부드러운 타겟 위치
    private Vector3 _smoothTargetPos;

    // 흔들림 변수
    private float _shakeTimer;
    private float _shakeMagnitude;
    private Vector3 _currentShakePos;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    void Start()
    {
        _cam = GetComponent<Camera>();
        _defaultFov = _cam.fieldOfView;

        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        CalculateStaticOffset();

        // 시작 시 위치 초기화
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

        // ================================================================
        // 1. [핵심 수정] X, Z는 빠르게, Y(높이)는 느리게 따라가서 떨림 제거
        // ================================================================
        float x = Mathf.Lerp(_smoothTargetPos.x, target.position.x, Time.deltaTime * smoothSpeed);
        float z = Mathf.Lerp(_smoothTargetPos.z, target.position.z, Time.deltaTime * smoothSpeed);

        // Y축은 아주 천천히 따라가게 하여 물리 진동을 무시함
        float y = Mathf.Lerp(_smoothTargetPos.y, target.position.y, Time.deltaTime * _heightSmoothSpeed);

        _smoothTargetPos = new Vector3(x, y, z);
        // ================================================================

        // 2. 줌 & 시야 이동
        bool isAiming = Input.GetMouseButton(1);
        float targetFov = isAiming ? zoomedFov : _defaultFov;
        float currentRatio = isAiming ? aimShiftRatio : baseShiftRatio;

        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, Time.deltaTime * zoomSpeed);

        Vector3 mousePos = GetMouseGroundPos();
        Vector3 dir = mousePos - target.position;
        Vector3 clampedDir = Vector3.ClampMagnitude(dir, maxShiftDistance);
        Vector3 targetShift = clampedDir * currentRatio;
        targetShift.y = 0;

        _currentShift = Vector3.Lerp(_currentShift, targetShift, Time.deltaTime * shiftSpeed);

        // 3. 흔들림(쉐이크)
        if (_shakeTimer > 0)
        {
            _currentShakePos = Random.insideUnitSphere * _shakeMagnitude;
            _shakeTimer -= Time.deltaTime;
        }
        else
        {
            _currentShakePos = Vector3.MoveTowards(_currentShakePos, Vector3.zero, Time.deltaTime * 5f);
        }

        // 4. 최종 적용
        Vector3 finalPos = (_smoothTargetPos + _currentShift) + _currentShakePos + _staticOffset;
        transform.position = finalPos;
        transform.rotation = Quaternion.Euler(xAngle, yAngle, 0);
    }

    public void Shake(float duration, float magnitude)
    {
        _shakeTimer = duration;
        _shakeMagnitude = magnitude;
    }

    private Vector3 GetMouseGroundPos()
    {
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, new Vector3(0, target.position.y, 0));
        if (ground.Raycast(ray, out float enter)) return ray.GetPoint(enter);
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