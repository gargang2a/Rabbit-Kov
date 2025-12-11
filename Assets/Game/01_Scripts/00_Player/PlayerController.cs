using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // === Inspector 변수 ===
    [Header("Movement Settings")]
    public float moveSpeed = 60f;              // 일반적인 플레이어 이동 속도
    public float dashMultiplier = 2.4f;        // 대쉬 속도 계수 (Shift를 누를 때)
    public float rotationSlerpSpeed = 10f;     // 회전 보간 속도 (현재 사용하지 않음)

    // 구르기 관련 변수
    public KeyCode rollKey = KeyCode.Space; // <--- 구르기에 사용할 키
    public float rollDuration = 0.5f;       // <--- 구르기 동작 지속 시간 (애니메이션 클립 길이와 일치하도록 설정)
    private bool canRoll = true;             // <--- 구르기 쿨타임/가용성 제어
    public float rollDistance;
    public bool isRolling = false; // 현재 구르기 중인지 체크
    private Vector3 rollVelocity;     // 구르기 시 적용할 속도 벡터


    // === 디버그 변수 ===
    [Header("Debug")]
    [SerializeField]
    private float currentMovementSpeed;
    [SerializeField]
    private bool isDashing = false;

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
        // 1. 회전 로직: Update에서 실행하여 마우스 입력과 즉시 동기화
        HandleRotation();
        HandleRollInput();
    }

    void FixedUpdate()
    {
        // 2. 이동 로직: FixedUpdate에서 물리적으로 처리
        if (isRolling)
        {
            // 구르기 중: 계산된 속도로 이동
            Vector3 targetPosition = rb.position + rollVelocity * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
        else
        {
            // 일반 상태: 기존 이동 로직 처리
            HandleMovement();
        }
    }

    // 1. 캐릭터 회전 로직: 마우스 포인터 바라보기 (떨림 최소화를 위해 즉시 회전)
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

                // 떨림 최소화 핵심 수정: Slerp 대신 즉시 회전
                transform.rotation = targetRotation;
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

        isDashing = Input.GetKey(KeyCode.LeftShift);

        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
        }

        // 4. 이동 처리 (Rigidbody.MovePosition 사용)
        if (moveDirection.magnitude >= 0.1f)
        {
            Vector3 targetVelocity = moveDirection * currentSpeed;
            Vector3 targetPosition = rb.position + targetVelocity * Time.fixedDeltaTime;

            rb.MovePosition(targetPosition);

            // 5. 인스펙터 디버그 변수 업데이트
            currentMovementSpeed = targetVelocity.magnitude;
        }
        else
        {
            // 움직임이 없을 때 Rigidbody 정지 (떨림 방지 핵심)
            rb.velocity = Vector3.zero;
            currentMovementSpeed = 0f;
        }
        if (!isRolling) // *** 이 조건 추가 ***
        {
            animator.SetFloat("Speed", currentMovementSpeed);
        }

        if (!isRolling) // *** 이 조건 추가 ***
        {
            // [추가된 부분 시작]
            animator.SetBool("IsDashing", isDashing); // IsDashing 상태 전달
            // [추가된 부분 끝]

            animator.SetFloat("Speed", currentMovementSpeed);
        }
    }

    // 3. 구르기 키 입력 처리
    void HandleRollInput()
    {
        if (Input.GetKeyDown(rollKey) && canRoll)
        {
            // 구르기 애니메이션 시작
            StartRoll();
        }
    }

    // 4. 구르기 시작 로직
    void StartRoll()
    {
        if (animator == null) return;

        canRoll = false; // 구르기 중에는 다시 구르지 못하도록
        isDashing = false; // 구르기 중 대쉬 상태 해제
        isRolling = true; // 구르기 상태 활성화


        // 1. WASD 입력 값 받기 (HandleMovement에서 사용하는 것과 동일)
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
        
        Vector3 rollDirection;
        // ----------------------------------------------------
        // *** 핵심 로직 수정: 구르기 방향 결정 ***
        // ----------------------------------------------------
        if (inputDirection.magnitude >= 0.1f)
        {
            // 2. 조건 2: 방향키를 누르고 있을 때
            rollDirection = inputDirection;

            // 구르기 방향으로 캐릭터 즉시 회전
            // (구르기 애니메이션과 물리 이동을 일치시키기 위해)
            Quaternion targetRotation = Quaternion.LookRotation(rollDirection);
            transform.rotation = targetRotation;

            // 이후 마우스 회전 복귀는 EndRollAfterDelay에서 처리 (아래 3번 참고)
        }
        else
        {
            // 2. 조건 1: 방향키를 누르지 않고 있을 때
            // 현재 플레이어가 바라보고 있는 방향 (마우스 포인터 방향)으로 구르기
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
        
        // 이 부분에 rollDistance를 활용해서 로컬축 정면 방향으로 이동하기

    }


    // 5. 구르기 상태를 해제하는 코루틴
    IEnumerator EndRollAfterDelay(float delay)
    {
        // 구르기 애니메이션 지속 시간만큼 기다림
        yield return new WaitForSeconds(delay);

        // Animator의 IsRolling bool 파라미터를 False로 설정하여 애니메이션 종료
        animator.SetBool("IsRolling", false);

        isRolling = false; // 구르기 상태 해제 <--- 이 부분 추가/수정
        rollVelocity = Vector3.zero; // 혹시 모를 잔여 속도 초기화

        // 필요하다면 구르기 쿨타임을 여기에서 추가할 수 있습니다.
        // yield return new WaitForSeconds(rollCooldown);

        canRoll = true; // 구르기 다시 가능하도록 설정

        // ----------------------------------------------------
        // *** 핵심 추가: 마우스 방향으로 부드럽게 회전 복귀 ***
        // ----------------------------------------------------

        // 목표 회전 계산 (현재 마우스가 가리키는 방향)
        Vector3 mouseDirection = GetMouseWorldDirection();

        // 부드러운 회전을 위한 코루틴 시작 (추가 코루틴 필요)
        if (mouseDirection != Vector3.zero)
        {
            StartCoroutine(SmoothRotateToMouse(mouseDirection));
        }
    }

    // *** 추가할 새로운 코루틴 ***
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

    IEnumerator SmoothRotateToMouse(Vector3 targetDirection)
    {
        Quaternion initialRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        float timer = 0f;
        float rotationTime = 0.2f; // 회전 복귀에 걸리는 시간 (원하는 속도로 설정)

        while (timer < rotationTime)
        {
            if (isRolling) yield break; // 혹시라도 구르기 도중에 다시 시작되면 중단

            // Slerp을 사용하여 부드럽게 회전
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, timer / rotationTime);
            timer += Time.deltaTime;
            yield return null;
        }

        // 최종적으로 목표 회전으로 설정 (오차 방지)
        transform.rotation = targetRotation;
    }
}