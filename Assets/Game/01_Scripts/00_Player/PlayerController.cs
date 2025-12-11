using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // === Inspector 변수 ===

    [Header("Movement Settings")]
    [Tooltip("캐릭터의 기본 이동 속도")]
    public float moveSpeed = 130f;

    [Tooltip("Shift를 누를 때 기본 속도에 곱해지는 계수 (대쉬 속도 조절)")]
    public float dashMultiplier = 1.6f;

    [Tooltip("캐릭터 회전 시 사용할 보간 속도 (현재 즉시 회전 사용으로 미사용 중)")]
    public float rotationSlerpSpeed = 10f;


    [Header("Roll (구르기) Settings")]
    [Tooltip("구르기 동작을 수행할 키")]
    public KeyCode rollKey = KeyCode.Space;

    [Tooltip("구르기 애니메이션이 진행되는 시간 (애니메이션 클립 길이와 일치하도록 설정)")]
    public float rollDuration = 0.5f;

    [Tooltip("구르기 동작으로 이동할 총 거리. RollDuration과 함께 구르기 속도를 결정합니다.")]
    public float rollDistance = 120f;


    // === 내부 상태 및 디버그 변수 ===

    [Header("Internal Status (Debug)")]
    [SerializeField, Tooltip("현재 물리 이동 속도 (FixedUpdate 기준)")]
    private float currentMovementSpeed;

    [SerializeField, Tooltip("현재 대쉬(Shift) 키를 누르고 있는 상태인지 여부")]
    private bool isDashing = false;

    [Tooltip("현재 구르기 애니메이션 및 물리 이동 중인지 여부")]
    public bool isRolling = false;

    // 내부 변수
    private Rigidbody rb;
    private Animator animator;
    private Camera mainCamera;

    // 구르기 제어 변수
    private bool canRoll = true;
    private Vector3 rollVelocity;

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
        // 1. 회전 로직: Update에서 실행하여 마우스 입력과 즉시 동기화
        HandleRotation();
        HandleRollInput();
    }

    void FixedUpdate()
    {
        // 2. 이동 로직: FixedUpdate에서 물리적으로 처리 (rb.velocity 사용)
        if (isRolling)
        {
            // 구르기 중: 계산된 속도로 이동
            rb.velocity = rollVelocity;
        }
        else
        {
            // 일반 상태: 기존 이동 로직 처리
            HandleMovement();
        }
    }

    // 1. 캐릭터 회전 로직: 마우스 포인터 바라보기
    void HandleRotation()
    {
        if (isRolling)
        {
            return;
        }

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
                transform.rotation = targetRotation;
            }
        }
    }

    // 2. 캐릭터 이동 로직: rb.velocity 기반 이동
    void HandleMovement()
    {
        // 1. WASD 입력 값 받기
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // 2. 월드 축 기준 이동 방향 벡터 생성
        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        // 3. 현재 이동 속도 결정 (대쉬 여부 체크)
        float currentSpeed = moveSpeed;
        isDashing = Input.GetKey(KeyCode.LeftShift);

        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
        }

        // 4. 이동 처리 (Rigidbody.velocity 사용)
        if (moveDirection.magnitude >= 0.1f)
        {
            Vector3 targetVelocity = moveDirection * currentSpeed;

            // rb.velocity 설정
            rb.velocity = targetVelocity;

            // 5. 인스펙터 디버그 변수 업데이트
            currentMovementSpeed = targetVelocity.magnitude;
        }
        else
        {
            // 움직임이 없을 때 Rigidbody 속도 0으로 설정
            rb.velocity = Vector3.zero;
            currentMovementSpeed = 0f;
        }

        if (!isRolling)
        {
            animator.SetBool("IsDashing", isDashing);
            animator.SetFloat("Speed", currentMovementSpeed);
        }
    }

    // 3. 구르기 키 입력 처리
    void HandleRollInput()
    {
        if (Input.GetKeyDown(rollKey) && canRoll && !isRolling)
        {
            StartRoll();
        }
    }

    // 4. 구르기 시작 로직
    void StartRoll()
    {
        if (animator == null) return;

        canRoll = false;
        isDashing = false;
        isRolling = true;

        // 1. WASD 입력 값 받기
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        Vector3 rollDirection;

        if (inputDirection.magnitude >= 0.1f)
        {
            // 방향키를 누르고 있을 때: 입력 방향으로 구르기
            rollDirection = inputDirection;

            // 구르기 방향으로 캐릭터 즉시 회전
            Quaternion targetRotation = Quaternion.LookRotation(rollDirection);
            transform.rotation = targetRotation;
        }
        else
        {
            // 방향키를 누르지 않고 있을 때: 현재 바라보고 있는 방향으로 구르기
            rollDirection = transform.forward;
        }

        // 2. 구르기 이동 속도 계산 (거리 / 시간 = 속도)
        float speed = rollDistance / rollDuration;

        // 3. 구르기 속도 벡터 설정
        rollVelocity = rollDirection * speed;

        // Animator의 IsRolling bool 파라미터를 True로 설정하여 애니메이션 시작
        animator.SetBool("IsRolling", true);

        // 구르기 애니메이션 길이만큼 대기 후, 구르기 상태 해제 코루틴 시작
        StartCoroutine(EndRollAfterDelay(rollDuration));
    }


    // 5. 구르기 상태를 해제하는 코루틴
    IEnumerator EndRollAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 구르기가 끝나면 속도를 0으로 설정하여 미끄러짐 방지
        rb.velocity = Vector3.zero;

        animator.SetBool("IsRolling", false);

        isRolling = false;
        rollVelocity = Vector3.zero;

        canRoll = true;

        // 마우스 방향으로 부드럽게 회전 복귀
        Vector3 mouseDirection = GetMouseWorldDirection();

        if (mouseDirection != Vector3.zero)
        {
            StartCoroutine(SmoothRotateToMouse(mouseDirection));
        }
    }

    // *** 마우스 방향 계산 유틸리티 함수 ***
    private Vector3 GetMouseWorldDirection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        float hitDistance;
        Vector3 directionToLook = Vector3.zero;

        if (groundPlane.Raycast(ray, out hitDistance))
        {
            Vector3 mouseWorldPosition = ray.GetPoint(hitDistance);
            directionToLook = mouseWorldPosition - transform.position;
            directionToLook.y = 0;
        }
        return directionToLook.normalized;
    }

    // *** 부드러운 회전 복귀 코루틴 ***
    IEnumerator SmoothRotateToMouse(Vector3 targetDirection)
    {
        Quaternion initialRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        float timer = 0f;
        float rotationTime = 0.2f; // 회전 복귀에 걸리는 시간

        while (timer < rotationTime)
        {
            if (isRolling) yield break;

            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, timer / rotationTime);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRotation;
    }
}