using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // === Inspector 변수 ===
    [Header("Movement Settings")]
    public float moveSpeed = 60f;           // 일반적인 플레이어 이동 속도
    public float dashMultiplier = 2.4f;     // 대쉬 속도 계수 (Shift를 누를 때 1.2배)
    public float rotationSlerpSpeed = 10f;  // 회전 보간 속도

    // === 디버그 변수 (Inspector에서 실시간 확인 가능) ===
    [Header("Debug")]
    [SerializeField]
    private float currentMovementSpeed; // <--- 인스펙터에 현재 속도 표시
    [SerializeField]
    private bool isDashing = false;    // <--- 현재 대쉬 중인지 표시

    // === 내부 변수 ===
    private Rigidbody rb;
    private Animator animator;
    private Camera mainCamera;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        if (rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody component!");
            return;
        }

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        HandleRotation();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    // 1. 캐릭터 회전 로직: 마우스 포인터 바라보기
    void HandleRotation()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        float hitDistance;

        if (groundPlane.Raycast(ray, out hitDistance))
        {
            Vector3 mouseWorldPosition = ray.GetPoint(hitDistance);
            Vector3 directionToLook = mouseWorldPosition - transform.position;
            directionToLook.y = 0;

            if (directionToLook != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToLook);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSlerpSpeed);
            }
        }
    }

    // 2. 캐릭터 이동 로직: 월드 축 기준 이동 (WASD) + 대쉬 기능
    void HandleMovement()
    {
        // 1. WASD 입력 값 받기
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // 2. 월드 축 기준 이동 방향 벡터 생성
        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        // 3. 현재 이동 속도 결정 (대쉬 여부 체크)
        float currentSpeed = moveSpeed;

        isDashing = Input.GetKey(KeyCode.LeftShift); // <--- 대쉬 상태 업데이트

        if (isDashing) // <--- isDashing 변수 사용
        {
            currentSpeed *= dashMultiplier;
            // Debug.Log($"대쉬 활성화! 현재 속도: {currentSpeed}"); // 디버그 로그 추가
        }

        // 4. 이동 처리 (Rigidbody.MovePosition 사용)
        if (moveDirection.magnitude >= 0.1f)
        {
            Vector3 targetVelocity = moveDirection * currentSpeed;
            Vector3 targetPosition = rb.position + targetVelocity * Time.fixedDeltaTime;

            rb.MovePosition(targetPosition);

            // 5. 인스펙터 디버그 변수 업데이트
            currentMovementSpeed = targetVelocity.magnitude; // <--- 속도 크기 업데이트
        }
        else
        {
            // 움직임이 없을 때 속도를 0으로 초기화
            currentMovementSpeed = 0f;
        }

        animator.SetFloat("Speed", currentMovementSpeed);
    }
}