using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // === Inspector 변수 ===
    [Header("Movement Settings")]
    public float moveSpeed; // 플레이어의 이동 속도
    public float dashMultiplier = 1.2f;
    public float normalSpeed;
    public float dashSpeed;
    [field: SerializeField] public float stamina { get; private set; } = 100;

    // === 내부 변수 ===
    private Rigidbody rb;
    private Camera mainCamera;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        if (rb != null)
        {
            // 탑뷰 게임에서 캐릭터가 넘어지는 것을 방지하기 위해 회전을 고정합니다.
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        }
    }

    void Update()
    {
        // 1. 회전 처리 (HandleRotation)
        // 마우스 커서를 따라 회전하는 것은 프레임 단위로 즉시 업데이트합니다.
        HandleRotation();
    }

    void FixedUpdate()
    {
        // 2. 이동 처리 (HandleMovement)
        // 물리 기반 이동은 FixedUpdate에서 처리하여 물리 엔진과 동기화합니다.
        HandleMovement();
    }

    // 1. 캐릭터 회전 로직: 마우스 포인터 바라보기
    void HandleRotation()
    {
        // 1. 스크린 상의 마우스 위치를 월드 좌표로 변환하기 위해 Raycast를 사용합니다.
        // 이는 탑뷰/쿼터뷰 게임에서 정확한 마우스 위치를 찾는 가장 일반적인 방법입니다.
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // 캐릭터의 Y 높이에 있는 가상의 평면(Ground Plane)을 생성합니다.
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        float hitDistance;

        // Ray가 평면과 교차하는지 확인합니다.
        if (groundPlane.Raycast(ray, out hitDistance))
        {
            // 2. Ray가 평면에 닿은 월드 좌표를 얻습니다.
            Vector3 mouseWorldPosition = ray.GetPoint(hitDistance);

            // 3. 방향 벡터 계산 (마우스 위치 - 캐릭터 위치)
            Vector3 directionToLook = mouseWorldPosition - transform.position;
            directionToLook.y = 0; // Y축은 무시하여 수평 회전만 수행합니다.

            if (directionToLook != Vector3.zero)
            {
                // 4. 방향 벡터를 바라보는 회전값 계산 후 즉시 적용합니다.
                Quaternion targetRotation = Quaternion.LookRotation(directionToLook);
                //transform.rotation = targetRotation;

                //(선택 사항) 부드러운 회전을 원하면 Slerp 사용:
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
    }

    // 2. 캐릭터 이동 로직: 월드 축 기준 이동 (WASD)
    void HandleMovement()
    {
        // 1. WASD 입력 값 받기
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // 2. 월드 축 기준 이동 방향 벡터 생성
        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        // 3. 현재 이동 속도 결정 (대쉬 여부 체크)
        normalSpeed = moveSpeed;
        dashSpeed = moveSpeed * dashMultiplier;

        // Shift 키를 누르면 속도 계수를 적용합니다.
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (stamina >= 0)
            {
                moveSpeed = dashSpeed;
                stamina = stamina - 0.1f;
            }

            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                moveSpeed = normalSpeed;
            }
        }
        // 4. 이동 처리 (Rigidbody.MovePosition 사용)
        if (moveDirection.magnitude >= 0.1f)
        {
            // 목표 위치 = 현재 위치 + (방향 * 속도 * 고정된 시간)
            Vector3 targetVelocity = moveDirection * moveSpeed;
            Vector3 targetPosition = rb.position + targetVelocity * Time.fixedDeltaTime;

            // Rigidbody를 사용하여 물리적으로 위치를 업데이트합니다.
            rb.MovePosition(targetPosition);
        }
    }
}
