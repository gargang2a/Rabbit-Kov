using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    // --- 기존 설정 변수 ---
    [Header("Target & Distance")]
    public Transform target; // 플레이어 오브젝트의 Transform
    public float distance = 500f; // 플레이어로부터의 거리 (기본 후퇴 거리)

    [Header("Angle Settings")]
    [Range(0f, 360f)]
    public float yAngle = 18f; // 수평 각도
    [Range(0f, 90f)]
    public float xAngle = 55f; // 수직 기울기 (높은 각도)

    [Header("Smoothing")]
    [Tooltip("카메라 추적 속도 및 플레이어 위치 필터링 속도")]
    public float smoothSpeed = 10f;

    // --- 마우스 오프셋 설정 변수 (핵심) ---
    [Header("Mouse Aim Offset Settings")]
    [Tooltip("플레이어-카메라 거리 대비, 마우스에 의해 이동 가능한 최대 오프셋 비율 (0.3f = 30%)")]
    public float maxOffsetFactor = 0.3f;
    public float offsetSpeed = 10f;       // 오프셋이 마우스를 따라가는 속도

    // --- 내부 변수 ---
    private Vector3 staticOffset;         // 플레이어 대비 고정 오프셋 (각도 기반)
    private Vector3 currentDynamicOffset; // 현재 동적 오프셋
    private Vector3 targetDynamicOffset;  // 목표 동적 오프셋

    // [추가] 카메라 떨림 완화를 위해 부드럽게 필터링된 플레이어 위치
    private Vector3 smoothedTargetPosition;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("카메라의 추적 대상(Target)이 설정되지 않았습니다.");
            enabled = false;
            return;
        }

        // 마우스 커서가 씬 뷰 밖으로 나가지 않도록 설정
        Cursor.lockState = CursorLockMode.Confined;

        CalculateStaticOffset();

        // [추가] 시작 시 목표 위치 초기화
        smoothedTargetPosition = target.position;

        transform.position = target.position + staticOffset;
        transform.LookAt(target);
    }

    /// <summary>
    /// 카메라 로직은 모든 Update()가 끝난 후 호출되어야 떨림을 방지할 수 있습니다.
    /// </summary>
    void FixedUpdate()
    {
        if (target == null) return;

        // 0. [떨림 완화 로직] 플레이어의 위치를 부드럽게 필터링합니다. 
        // target.position의 급격한 변화를 늦춰서 smoothedTargetPosition에 적용합니다.
        smoothedTargetPosition = Vector3.Lerp(
            smoothedTargetPosition,
            target.position,
            Time.deltaTime * smoothSpeed // smoothSpeed를 필터링 속도로 활용
        );


        // 1. 마우스가 가리키는 지점(Look Point) 계산
        Vector3 lookPoint = GetMouseLookPoint();

        // 2. 마우스 위치를 기반으로 목표 동적 오프셋 계산
        CalculateTargetDynamicOffset(lookPoint);

        // 3. 동적 오프셋을 목표치로 부드럽게 이동
        currentDynamicOffset = Vector3.Lerp(currentDynamicOffset, targetDynamicOffset, Time.deltaTime * offsetSpeed);

        // === 핵심 카메라 위치 및 시선 계산 ===

        // [수정] 플레이어 위치 대신 필터링된 위치(smoothedTargetPosition)를 사용합니다.
        // 플레이어 위치와 동적 오프셋의 중간 지점을 새로운 중심점으로 설정합니다.
        Vector3 newFocusPoint = smoothedTargetPosition + currentDynamicOffset * 0.5f;

        // 4. 최종 목표 위치 계산 (새로운 중심점 + 고정 오프셋)
        Vector3 desiredPosition = newFocusPoint + staticOffset;

        // 5. Lerp를 사용하여 카메라 위치를 부드럽게 추적
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 6. 카메라가 새로운 중심점(플레이어와 오프셋의 중간)을 바라보도록 조정
        transform.LookAt(newFocusPoint);
    }

    /// <summary>
    /// 설정된 각도와 거리를 기반으로 고정 오프셋을 계산합니다.
    /// </summary>
    private void CalculateStaticOffset()
    {
        Quaternion rotation = Quaternion.Euler(xAngle, yAngle, 0);
        Vector3 direction = rotation * Vector3.back;
        staticOffset = direction * distance;
    }

    /// <summary>
    /// 마우스 커서와 플레이어의 월드 좌표를 사용하여 목표 오프셋을 계산합니다.
    /// </summary>
    private void CalculateTargetDynamicOffset(Vector3 lookPoint)
    {
        // 1. 플레이어에서 마우스 지점까지의 벡터를 계산합니다.
        Vector3 displacement = lookPoint - target.position;
        displacement.y = 0; // 수직 성분 무시

        // 2. 목표 동적 오프셋은 마우스 지점 방향으로의 벡터를 클램핑한 값입니다.
        // maxOffsetFactor만큼 카메라가 플레이어 주변에서 움직일 수 있도록 합니다.
        targetDynamicOffset = Vector3.ClampMagnitude(displacement, distance * maxOffsetFactor);

        // 3. 시점의 쏠림이 덜 강해야 자연스러우므로, 절반 정도만 적용합니다.
        targetDynamicOffset *= 0.5f;
    }

    /// <summary>
    /// 마우스 커서의 씬 내 월드 좌표를 반환합니다 (Y=플레이어 높이 평면 기준).
    /// </summary>
    private Vector3 GetMouseLookPoint()
    {
        // Raycast를 위한 카메라 컴포넌트 가져오기
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        // 플레이어의 Y축을 기준으로 하는 평면을 생성합니다.
        Plane groundPlane = new Plane(Vector3.up, target.position);

        if (groundPlane.Raycast(ray, out float distanceToPlane))
        {
            return ray.GetPoint(distanceToPlane);
        }

        // Raycast 실패 시 플레이어 위치 반환
        return target.position;
    }

    /// <summary>
    /// 스크립트 설정값 변경 시 실시간으로 카메라 오프셋을 업데이트합니다. (에디터 전용)
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && target != null)
        {
            CalculateStaticOffset();
        }
    }
}