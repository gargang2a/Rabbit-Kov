using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // === Inspector Settings ===
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 12f;
    [SerializeField] private float _dashMultiplier = 1.8f;
    [SerializeField] private float _rotationSpeed = 540f;
    [SerializeField] private float _gravity = -30f;

    [Header("Roll Settings")]
    [SerializeField] private KeyCode _rollKey = KeyCode.Space;
    [SerializeField] private float _rollDuration = 0.5f;
    [SerializeField] private float _rollCooldown = 0f;
    [SerializeField] private float _rollDistance = 15f;

    [Header("Dead Zone")]
    [SerializeField] private float _minRotationDistance = 1.0f;

    [Header("Internal State")]
    [SerializeField] private bool _canRoll = true;
    [SerializeField] private bool _isRolling = false;
    [SerializeField] private bool _isDashing = false;
    [SerializeField]private bool _isGrounded;
    private Vector3 _rollVelocity;
    private Vector3 _verticalVelocity;

    // === References ===
    private CharacterController _controller;
    private Rigidbody _rb;
    private Animator _animator;
    private Camera _mainCamera;
    private PlayerAttack _playerAttack;

    // === Public Properties ===
    public bool IsRolling => _isRolling;

    private void Reset()
    {
        _moveSpeed = 12f;
    }

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _playerAttack = GetComponent<PlayerAttack>();
        _mainCamera = Camera.main;

        if (_rb != null)
        {
            _rb.isKinematic = true; // 물리 연산 충돌 방지
            _rb.useGravity = false;
        }
    }

    void Update()
    {
        ApplyGravity();
        HandleRotation();
        HandleRollInput();

        if (!_isRolling)
        {
            HandleMovement();
        }
        else
        {
            HandleRollMovement();
        }

        // CharacterController와 Rigidbody 위치 동기화 (떨림 방지)
        if (_rb != null) _rb.position = transform.position;
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded)
        {
            _isGrounded = true;
            _verticalVelocity.y = -0.5f; // 접지 상태 유지용 미세 중력
        }
        else
        {
            _isGrounded = false;
            _verticalVelocity.y += _gravity * Time.deltaTime;
        }
    }

    private void HandleRotation()
    {
        if (_isRolling) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        float hitDistance;

        if (groundPlane.Raycast(ray, out hitDistance))
        {
            Vector3 mouseWorldPosition = ray.GetPoint(hitDistance);
            Vector3 directionToLook = mouseWorldPosition - transform.position;
            directionToLook.y = 0;

            if (directionToLook.magnitude < _minRotationDistance) return;

            Quaternion targetRotation = Quaternion.LookRotation(directionToLook);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime
            );
        }
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 moveDir = new Vector3(h, 0f, v).normalized;

        float currentSpeed = _moveSpeed;
        _isDashing = Input.GetKey(KeyCode.LeftShift);

        if (_isDashing) currentSpeed *= _dashMultiplier;

        Vector3 finalMove = _verticalVelocity;

        if (moveDir.magnitude >= 0.1f)
        {
            Vector3 horizontalVelocity = moveDir * currentSpeed;
            finalMove += horizontalVelocity;

            // 애니메이션 파라미터
            _animator.SetFloat("Speed", horizontalVelocity.magnitude);
        }
        else
        {
            _animator.SetFloat("Speed", 0f);
        }

        _controller.Move(finalMove * Time.deltaTime);
        _animator.SetBool("IsDashing", _isDashing);
    }

    private void HandleRollInput()
    {
        // 공격 중이 아닐 때만 구르기 가능
        if (Input.GetKeyDown(_rollKey) && _canRoll && !_playerAttack.IsAttacking)
        {
            StartRoll();
        }
    }

    private void StartRoll()
    {
        _canRoll = false;
        _isDashing = false;
        _isRolling = true;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        // 이동 입력이 없으면 캐릭터가 보는 방향으로 구르기
        Vector3 rollDir = (inputDir.magnitude >= 0.1f) ? inputDir : transform.forward;

        // 구르는 방향으로 즉시 회전
        transform.rotation = Quaternion.LookRotation(rollDir);

        float speed = _rollDistance / _rollDuration;
        _rollVelocity = rollDir * speed;

        _animator.SetBool("IsRolling", true);
        StartCoroutine(EndRollRoutine(_rollDuration));
    }

    private void HandleRollMovement()
    {
        _controller.Move((_rollVelocity + _verticalVelocity) * Time.deltaTime);
    }

    private IEnumerator EndRollRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        _animator.SetBool("IsRolling", false);
        _isRolling = false;
        _rollVelocity = Vector3.zero;

        if (_rollCooldown > 0f)
            yield return new WaitForSeconds(_rollCooldown);

        _canRoll = true;
    }
}