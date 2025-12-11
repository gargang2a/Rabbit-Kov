using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // === Inspector 변수 ===
    [Header("Movement Settings")]
    public float moveSpeed = 60f;
    public float dashMultiplier = 2.4f;
    public float rotationSpeed = 540f;
    public float gravity = -30f;

    // === Dead Zone 설정 변수 ===
    [Header("Rotation Dead Zone")]
    public float minRotationDistance = 1.0f;

    // 구르기 관련 변수
    public KeyCode rollKey = KeyCode.Space;
    public float rollDuration = 0.5f;
    public float rollCooldown = 0f;
    private bool canRoll = true;
    public float rollDistance = 15f;
    public bool isRolling = false;
    private Vector3 rollVelocity;

    // === 디버그 변수 ===
    [Header("Debug")]
    [SerializeField]
    private float currentMovementSpeed;
    [SerializeField]
    private bool isDashing = false;

    // === 내부 변수 ===
    private CharacterController controller;
    private Rigidbody rb;
    private Animator animator;
    private Camera mainCamera;
    private Vector3 verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        if (controller == null)
        {
            Debug.LogError("PlayerController requires a CharacterController component!");
            return;
        }

        if (rb != null)
        {
            // Rigidbody가 이동을 방해하지 않도록 설정
            rb.isKinematic = true;
            // Unity Inspector에서 Rigidbody -> Interpolate: None, Use Gravity: 체크 해제 필수!
        }
    }

    void Start()
    {
        if (animator != null)
        {
            animator.SetBool("IsRolling", false);
            animator.SetBool("IsDashing", false);
            animator.SetFloat("Speed", 0f);
        }
    }

    void Update()
    {
        // 1. 중력 적용
        ApplyGravity();

        // 2. 입력 및 회전 처리
        HandleRotation();
        HandleRollInput();

        // 3. 이동 로직 실행
        if (!isRolling)
        {
            HandleMovement();
        }
        else
        {
            HandleRollMovement();
        }

        // 4. *** 핵심 수정: Update 마지막에 Rigidbody 위치 동기화 ***
        if (rb != null)
        {
            // CharacterController의 최종 위치를 Rigidbody의 위치로 설정하여 떨림 방지
            rb.position = transform.position;
        }
    }

    // FixedUpdate는 Rigidbody의 물리 시뮬레이션 프레임을 제공하기 위해 남겨두되, 
    // 위치 조작은 하지 않습니다.
    void FixedUpdate()
    {
        // 비워 둠: Rigidbody 위치 조정은 Update에서 처리됩니다.
    }

    // 0. CharacterController에 중력 적용
    void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            verticalVelocity.y = -0.5f;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }
    }

    // 1. 캐릭터 회전 로직
    void HandleRotation()
    {
        if (isRolling)
        {
            return;
        }
        // ... (회전 로직 생략, 이전 코드와 동일)
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        float hitDistance;

        if (groundPlane.Raycast(ray, out hitDistance))
        {
            Vector3 mouseWorldPosition = ray.GetPoint(hitDistance);
            Vector3 directionToLook = mouseWorldPosition - transform.position;
            directionToLook.y = 0;

            if (directionToLook.magnitude < minRotationDistance)
            {
                return;
            }

            if (directionToLook != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToLook);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    // 2. 캐릭터 일반 이동 로직
    void HandleMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        float currentSpeed = moveSpeed;
        isDashing = Input.GetKey(KeyCode.LeftShift);

        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
        }

        Vector3 finalMovement = verticalVelocity;

        if (moveDirection.magnitude >= 0.1f)
        {
            Vector3 horizontalVelocity = moveDirection * currentSpeed;

            finalMovement = horizontalVelocity + verticalVelocity;

            currentMovementSpeed = horizontalVelocity.magnitude;
        }
        else
        {
            currentMovementSpeed = 0f;
        }

        controller.Move(finalMovement * Time.deltaTime);

        if (!isRolling)
        {
            animator.SetBool("IsDashing", isDashing);
            animator.SetFloat("Speed", currentMovementSpeed);
        }
    }

    // 3. 구르기 이동 로직
    void HandleRollMovement()
    {
        Vector3 finalRollMovement = rollVelocity + verticalVelocity;
        controller.Move(finalRollMovement * Time.deltaTime);
    }

    void HandleRollInput()
    {
        if (Input.GetKeyDown(rollKey) && canRoll)
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

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
        Vector3 rollDirection;

        if (inputDirection.magnitude >= 0.1f)
        {
            rollDirection = inputDirection;
            Quaternion targetRotation = Quaternion.LookRotation(rollDirection);
            transform.rotation = targetRotation;
        }
        else
        {
            rollDirection = transform.forward;
        }

        float speed = rollDistance / rollDuration;
        rollVelocity = rollDirection * speed;

        animator.SetBool("IsRolling", true);

        StartCoroutine(EndRollAfterDelay(rollDuration));
    }


    // 5. 구르기 상태를 해제하는 코루틴
    IEnumerator EndRollAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        animator.SetBool("IsRolling", false);
        isRolling = false;
        rollVelocity = Vector3.zero;

        if (rollCooldown > 0f)
        {
            yield return new WaitForSeconds(rollCooldown);
        }

        canRoll = true;
    }
}