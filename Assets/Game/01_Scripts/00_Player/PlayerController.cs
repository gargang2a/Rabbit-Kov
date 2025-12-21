using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private bool _canMove = true;

    // === Inspector Settings ===
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 16f;
    [SerializeField] private float _dashMultiplier = 1.5f;
    [SerializeField] private float _rotationSpeed = 720f;
    [SerializeField] private float _gravity = -30f;

    [Header("Slope Settings")]
    [Tooltip("경사면에서 미끄러지는 속도")]
    [SerializeField] private float _slideSpeed = 15f;
    [Tooltip("레이캐스트 길이 (키 절반 + 여유분)")]
    [SerializeField] private float _rayLengthOffset = 1.0f;
    [Tooltip("땅만 감지하기 위한 레이어 설정")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("Stamina Settings")]
    [SerializeField] private float _dashStaminaCost = 15f;
    [SerializeField] private float _runRecoveryThreshold = 20f;

    [Header("Roll Settings")]
    [SerializeField] private KeyCode _rollKey = KeyCode.Space;
    [SerializeField] private float _rollDuration = 0.5f;
    [SerializeField] private float _rollCooldown = 0.3f;
    [SerializeField] private float _rollDistance = 12f; // 기본 구르기 거리 (기본 속도일 때)
    [SerializeField] private int _rollStaminaCost = 25;

    [Header("Dead Zone")]
    [SerializeField] private float _minRotationDistance = 1.0f;

    [Header("Internal State")]
    [SerializeField] private bool _canRoll = true;
    [SerializeField] private bool _isRolling = false;
    [SerializeField] private bool _isDashing = false;
    [SerializeField] private bool _isGrounded;
    [SerializeField] private bool _isSliding = false;
    [SerializeField] private bool _rayHitGround = false;

    // 달리기 잠금 상태
    [SerializeField] private bool _isRunLocked = false;

    // ★ [신규 추가] 탄력 계산을 위한 초기 속도 저장용
    private float _initialMoveSpeed;

    private Vector3 _rollVelocity;
    private Vector3 _verticalVelocity;
    private Vector3 _slideVelocity;

    // === References ===
    private CharacterController _controller;
    private Rigidbody _rb;
    private Animator _animator;
    private Camera _mainCamera;
    private Player _playerStats;

    public bool IsRolling => _isRolling;

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

        // ★ [신규 추가] 게임 시작 시점의 기본 속도를 기준점으로 저장
        // 0으로 나뉘는 것을 방지하기 위해 최소값 보정
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

        if (!_isRolling)
        {
            HandleMovement();
        }
        else
        {
            HandleRollMovement();
        }

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
            Debug.DrawLine(rayOrigin, hit.point, Color.green);

            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (angle > _controller.slopeLimit)
            {
                _isSliding = true;
                Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                _slideVelocity = slopeDir * _slideSpeed;
            }
        }
        else
        {
            _rayHitGround = false;
            Debug.DrawRay(rayOrigin, Vector3.down * rayLen, Color.red);
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
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
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

    // ★ [핵심 수정] 속도에 비례하여 구르기 탄력 적용
    private void StartRoll()
    {
        _canRoll = false;
        _isDashing = false;
        _isRolling = true;

        float h = 0f; float v = 0f;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        Vector3 rollDir = (inputDir.magnitude >= 0.1f) ? inputDir : transform.forward;
        transform.rotation = Quaternion.LookRotation(rollDir);

        // 1. 속도 증가 비율 계산 (현재 기본 속도 / 초기 설정 속도)
        // 예: 속도가 16 -> 24로 증가했다면 비율은 1.5
        float speedRatio = _moveSpeed / _initialMoveSpeed;

        // 2. 무게 페널티 적용 (무거우면 구르기도 짧아짐)
        if (_playerStats != null)
        {
            speedRatio *= _playerStats.GetMoveSpeedMultiplier();
        }

        // 3. 최종 구르기 거리 계산 (기본 거리 * 비율)
        float finalRollDistance = _rollDistance * speedRatio;

        // 4. 속도 적용 (거리를 시간으로 나누어 속도 산출)
        _rollVelocity = rollDir * (finalRollDistance / _rollDuration);

        _animator.SetBool("IsRolling", true);
        StartCoroutine(EndRollRoutine(_rollDuration));
    }

    private void HandleMovement()
    {
        float h = 0f; float v = 0f;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;

        Vector3 moveDir = new Vector3(h, 0f, v).normalized;
        bool isMoving = moveDir.magnitude >= 0.1f;
        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift);

        if (_isRunLocked)
        {
            if (_playerStats != null && _playerStats.Stamina >= _runRecoveryThreshold)
                _isRunLocked = false;
        }

        if (isMoving && isShiftHeld && !_isRunLocked)
        {
            if (_playerStats != null && _playerStats.Stamina > 0)
            {
                _isDashing = true;
                _playerStats.ConsumeStamina(_dashStaminaCost * Time.deltaTime);
                if (_playerStats.Stamina <= 0) { _isRunLocked = true; _isDashing = false; }
            }
            else _isDashing = false;
        }
        else _isDashing = false;

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

    public void UpgradeSpeed(float amount) { _moveSpeed += amount; }
    public float GetMoveSpeed() { return _moveSpeed; }
}