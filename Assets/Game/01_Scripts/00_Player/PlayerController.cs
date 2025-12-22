using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private bool _canMove = true;

    // === Inspector Settings ===
    [Header("Movement Settings / 이동 설정")]
    [Tooltip("기본 이동 속도 (유닛: m/s)")]
    [SerializeField] private float _moveSpeed = 16f;
    [Tooltip("대시(달리기) 시 곱해지는 속도 배율")]
    [SerializeField] private float _dashMultiplier = 1.5f;
    [Tooltip("회전 속도 (도/초)")]
    [SerializeField] private float _rotationSpeed = 720f;
    [Tooltip("중력 가속도")]
    [SerializeField] private float _gravity = -30f;

    [Header("Slope Settings")]
    [SerializeField] private float _slideSpeed = 15f;
    [SerializeField] private float _rayLengthOffset = 1.0f;
    [SerializeField] private LayerMask _groundLayer;

    [Header("Stamina & Roll Settings")]
    [SerializeField] private float _dashStaminaCost = 15f;
    [SerializeField] private float _runRecoveryThreshold = 20f;
    [SerializeField] private KeyCode _rollKey = KeyCode.Space;
    [SerializeField] private float _rollDuration = 0.5f;
    [SerializeField] private float _rollCooldown = 0.3f;
    [SerializeField] private float _rollDistance = 12f;
    [SerializeField] private int _rollStaminaCost = 25;

    [Header("Dead Zone")]
    [SerializeField] private float _minRotationDistance = 1.0f;

    // 내부 상태 변수들
    private bool _canRoll = true;
    private bool _isRolling = false;
    private bool _isDashing = false;
    private bool _isGrounded;
    private bool _isSliding = false;
    private bool _rayHitGround = false;
    private bool _isRunLocked = false;

    private float _initialMoveSpeed;
    private Vector3 _rollVelocity;
    private Vector3 _verticalVelocity;
    private Vector3 _slideVelocity;

    // References
    private CharacterController _controller;
    private Rigidbody _rb;
    private Animator _animator;
    private Camera _mainCamera;
    private Player _playerStats;

    public bool IsRolling => _isRolling;

    // ★ 현재 실제 이동속도 (무게 적용됨)
    public float CurrentMoveSpeed
    {
        get
        {
            float speed = _moveSpeed;
            if (_playerStats != null)
            {
                speed *= _playerStats.GetMoveSpeedMultiplier();
            }
            return speed;
        }
    }

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _playerStats = GetComponent<Player>();
        _mainCamera = Camera.main;

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.None;
        }

        _initialMoveSpeed = Mathf.Max(_moveSpeed, 0.1f);
    }

    void Update()
    {
        if (!_canMove) return;

        if (_playerStats != null && _playerStats.IsDead)
        {
            _animator.SetFloat("Speed", 0f);
            return;
        }

        CalculateSlopeSlide();
        ApplyGravity();
        HandleRotation();
        HandleRollInput();

        if (!_isRolling) HandleMovement();
        else HandleRollMovement();

        if (_rb != null) _rb.position = transform.position;
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _rayHitGround)
        {
            _isGrounded = true;
            _verticalVelocity.y = -5f;
        }
        else
        {
            _isGrounded = false;
            _verticalVelocity.y += _gravity * Time.deltaTime;
        }
    }

    private void CalculateSlopeSlide()
    {
        _slideVelocity = Vector3.zero;
        _isSliding = false;
        _rayHitGround = false;

        Vector3 rayOrigin = transform.position + _controller.center;
        float rayLen = (_controller.height * 0.5f) + _rayLengthOffset;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLen, _groundLayer, QueryTriggerInteraction.Ignore))
        {
            _rayHitGround = true;
            if (Vector3.Angle(hit.normal, Vector3.up) > _controller.slopeLimit)
            {
                _isSliding = true;
                Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                _slideVelocity = slopeDir * _slideSpeed;
            }
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
            Vector3 targetDir = ray.GetPoint(hitDistance) - transform.position;
            targetDir.y = 0;
            if (targetDir.magnitude < _minRotationDistance) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(targetDir), _rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleRollInput()
    {
        if (Input.GetKeyDown(_rollKey) && _canRoll)
        {
            if (_playerStats != null && _playerStats.UseStamina(_rollStaminaCost))
                StartRoll();
        }
    }

    private void StartRoll()
    {
        _canRoll = false;
        _isDashing = false;
        _isRolling = true;

        Vector3 inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        Vector3 rollDir = inputDir.sqrMagnitude > 0.01f ? inputDir : transform.forward;
        transform.rotation = Quaternion.LookRotation(rollDir);

        float speedRatio = _moveSpeed / _initialMoveSpeed;
        if (_playerStats != null) speedRatio *= _playerStats.GetMoveSpeedMultiplier();

        float finalRollDistance = _rollDistance * speedRatio;
        _rollVelocity = rollDir * (finalRollDistance / _rollDuration);

        _animator.SetBool("IsRolling", true);
        StartCoroutine(EndRollRoutine(_rollDuration));
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 moveDir = new Vector3(h, 0, v).normalized;
        bool isMoving = moveDir.sqrMagnitude > 0.01f;

        if (_isRunLocked && _playerStats != null && _playerStats.Stamina >= _runRecoveryThreshold)
            _isRunLocked = false;

        _isDashing = isMoving && Input.GetKey(KeyCode.LeftShift) && !_isRunLocked;
        if (_isDashing && _playerStats != null)
        {
            _playerStats.ConsumeStamina(_dashStaminaCost * Time.deltaTime);
            if (_playerStats.Stamina <= 0) { _isRunLocked = true; _isDashing = false; }
        }

        // 현재 속도 계산 (CurrentMoveSpeed 프로퍼티와 동일 로직)
        float currentSpeed = _moveSpeed;
        if (_isDashing) currentSpeed *= _dashMultiplier;
        if (_playerStats != null) currentSpeed *= _playerStats.GetMoveSpeedMultiplier();

        Vector3 finalMove = _verticalVelocity + _slideVelocity;
        if (isMoving)
        {
            Vector3 horizontalVelocity = moveDir * currentSpeed;
            finalMove += horizontalVelocity;
            _animator.SetFloat("Speed", horizontalVelocity.magnitude);
        }
        else
        {
            _animator.SetFloat("Speed", 0f);
        }

        _controller.Move(finalMove * Time.deltaTime);
        _animator.SetBool("IsDashing", _isDashing);
    }

    private void HandleRollMovement()
    {
        _controller.Move((_rollVelocity + _verticalVelocity + _slideVelocity) * Time.deltaTime);
    }

    private IEnumerator EndRollRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        _animator.SetBool("IsRolling", false);
        _isRolling = false;
        _rollVelocity = Vector3.zero;
        if (_rollCooldown > 0f) yield return new WaitForSeconds(_rollCooldown);
        _canRoll = true;
    }

    // 업그레이드
    public void UpgradeSpeed(float amount)
    {
        _moveSpeed += amount;
    }

    // ★ [Fix] 기존 UI 스크립트들이 찾는 함수를 다시 복구함!
    // 이름만 GetMoveSpeed이고, 실제로는 최신 CurrentMoveSpeed 값을 리턴해줍니다.
    public float GetMoveSpeed()
    {
        return CurrentMoveSpeed;
    }
}