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
    [SerializeField] private float _rollDistance = 12f;
    [SerializeField] private int _rollStaminaCost = 25;

    [Header("Dead Zone")]
    [SerializeField] private float _minRotationDistance = 1.0f;

    [Header("Internal State")]
    [SerializeField] private bool _canRoll = true;
    [SerializeField] private bool _isRolling = false;
    [SerializeField] private bool _isDashing = false;
    [SerializeField] private bool _isGrounded;
    [SerializeField] private bool _isSliding = false;
    [SerializeField] private bool _rayHitGround = false; // ★ 추가됨: 레이가 땅에 닿았는지 여부

    // 달리기 잠금 상태
    [SerializeField] private bool _isRunLocked = false;

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
    }

    void Update()
    {
        if (!_canMove) return;

        if (_playerStats != null && _playerStats.IsDead)
        {
            _animator.SetFloat("Speed", 0f);
            return;
        }

        // 순서 중요: 슬라이드 계산을 먼저 해서 _rayHitGround 값을 갱신해야 함
        CalculateSlopeSlide();
        ApplyGravity(); // ★ 수정된 중력 로직 적용

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

    // ★ [핵심 수정 1] 중력 적용 로직 변경
    private void ApplyGravity()
    {
        // 1. 캐릭터 컨트롤러는 땅에 닿았다고 하지만(_isGrounded)
        // 2. 실제 발 밑 레이캐스트는 허공이라면(_rayHitGround == false)
        // -> 모서리에 걸린 상태이므로 강제로 떨어뜨려야 함!

        if (_controller.isGrounded && _rayHitGround)
        {
            _isGrounded = true;
            _verticalVelocity.y = -5f; // 땅에 잘 서있을 때만 붙어있는 힘 적용
        }
        else
        {
            _isGrounded = false;
            _verticalVelocity.y += _gravity * Time.deltaTime; // 그 외엔 무조건 중력 가속
        }
    }

    // ★ [핵심 수정 2] 레이캐스트 로직 보완
    private void CalculateSlopeSlide()
    {
        _slideVelocity = Vector3.zero;
        _isSliding = false;
        _rayHitGround = false; // 일단 거짓으로 초기화

        Vector3 rayOrigin = transform.position + _controller.center;
        float rayLen = (_controller.height * 0.5f) + _rayLengthOffset;

        RaycastHit hit;

        // QueryTriggerInteraction.Ignore: 킬존 같은 트리거(Trigger)는 무시하고 실제 땅(Collider)만 체크
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLen, _groundLayer, QueryTriggerInteraction.Ignore))
        {
            _rayHitGround = true; // 땅 찾음!
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
            _rayHitGround = false; // 땅 못 찾음 (허공)
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

        _rollVelocity = rollDir * (_rollDistance / _rollDuration);
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