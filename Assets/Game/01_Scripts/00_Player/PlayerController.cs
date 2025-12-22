using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // === Settings ===
    [Header("Movement")]
    [Tooltip("기본 이동 속도")]
    [SerializeField] private float _moveSpeed = 16f;
    [Tooltip("달리기 배율")]
    [SerializeField] private float _dashMultiplier = 1.5f;
    [Tooltip("회전 속도")]
    [SerializeField] private float _rotationSpeed = 720f;
    [Tooltip("중력 (낮을수록 빨리 떨어짐)")]
    [SerializeField] private float _gravity = -30f;
    [Tooltip("최대 낙하 속도 제한")]
    [SerializeField] private float _terminalVelocity = -50f;

    [Header("Rotation Fix (중요)")]
    [Tooltip("마우스가 감지할 레이어 (Floor만 체크하세요!)")]
    [SerializeField] private LayerMask _rotationLayerMask;

    [Header("Ground & Roll")]
    [SerializeField] private float _slideSpeed = 15f;
    [SerializeField] private float _rayLengthOffset = 0.2f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private KeyCode _rollKey = KeyCode.Space;
    [SerializeField] private float _rollDuration = 0.5f;
    [SerializeField] private float _rollCooldown = 0.3f;
    [SerializeField] private float _rollDistance = 12f;
    [SerializeField] private int _rollStaminaCost = 25;
    [SerializeField] private float _dashStaminaCost = 15f;

    // === Internal ===
    private bool _canRoll = true;
    private bool _isRolling = false;
    private bool _isDashing = false;
    private bool _isGrounded;
    private float _initialMoveSpeed;
    private Vector3 _verticalVelocity;
    private Vector3 _impactVelocity;

    private CharacterController _controller;
    private Animator _animator;
    private Player _playerStats;
    private Camera _mainCamera;

    public bool IsRolling => _isRolling;

    public float CurrentMoveSpeed
    {
        get
        {
            float speed = _moveSpeed;
            if (_playerStats != null) speed *= _playerStats.GetMoveSpeedMultiplier();
            return speed;
        }
    }

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _playerStats = GetComponent<Player>();
        _mainCamera = Camera.main;
        _initialMoveSpeed = Mathf.Max(_moveSpeed, 0.1f);
    }

    void Update()
    {
        if (_playerStats != null && _playerStats.IsDead) return;

        ApplyGravity();
        HandleRotation(); // ★ 수정된 회전 로직 실행
        HandleRollInput();
        HandleImpact();

        if (!_isRolling) HandleMovement();
        else HandleRollMovement();
    }

    // ==========================================
    // ★ [핵심 수정] 마우스 회전 로직
    // ==========================================
    private void HandleRotation()
    {
        if (_isRolling) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Plane 수학 계산 대신 물리 Raycast 사용
        // _rotationLayerMask에 체크된 레이어(Floor)만 감지하므로, 플레이어 몸을 통과함
        if (Physics.Raycast(ray, out hit, 1000f, _rotationLayerMask))
        {
            Vector3 targetPoint = hit.point;
            Vector3 direction = targetPoint - transform.position;
            direction.y = 0; // 높이 무시

            // 너무 가까우면 회전 안 함 (떨림 방지 2차)
            if (direction.sqrMagnitude < 0.1f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    // ==========================================
    // 물리 및 이동 로직
    // ==========================================
    public void ApplyKnockback(Vector3 force)
    {
        _impactVelocity += force;
    }

    public void ApplyLaunch(Vector3 launchForce)
    {
        _impactVelocity += launchForce;
        if (launchForce.y > 0)
        {
            _isGrounded = false;
            _verticalVelocity.y = 0;
        }
    }

    public void ApplyLaunch(float upwardForce)
    {
        ApplyLaunch(Vector3.up * upwardForce);
    }

    private void HandleImpact()
    {
        if (_impactVelocity.magnitude > 0.2f)
        {
            _controller.Move(_impactVelocity * Time.deltaTime);
            _impactVelocity = Vector3.Lerp(_impactVelocity, Vector3.zero, 5 * Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        bool rayHitGround = Physics.Raycast(transform.position + _controller.center, Vector3.down, (_controller.height * 0.5f) + _rayLengthOffset, _groundLayer);

        if (_controller.isGrounded && rayHitGround)
        {
            _isGrounded = true;
            _verticalVelocity.y = -5f;
        }
        else
        {
            _isGrounded = false;
            _verticalVelocity.y += _gravity * Time.deltaTime;
            if (_verticalVelocity.y < _terminalVelocity) _verticalVelocity.y = _terminalVelocity;
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
        _isRolling = true;
        _isDashing = false;

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        Vector3 dir = input.sqrMagnitude > 0.01f ? input : transform.forward;
        transform.rotation = Quaternion.LookRotation(dir);

        float speedRatio = _moveSpeed / _initialMoveSpeed;
        if (_playerStats != null) speedRatio *= _playerStats.GetMoveSpeedMultiplier();

        StartCoroutine(RollRoutine(dir, speedRatio));
    }

    private IEnumerator RollRoutine(Vector3 dir, float speedRatio)
    {
        _animator.SetBool("IsRolling", true);
        float elapsed = 0f;
        while (elapsed < _rollDuration)
        {
            elapsed += Time.deltaTime;
            Vector3 move = dir * (_rollDistance * speedRatio / _rollDuration);
            _controller.Move((move + _verticalVelocity) * Time.deltaTime);
            yield return null;
        }
        _animator.SetBool("IsRolling", false);
        _isRolling = false;
        yield return new WaitForSeconds(_rollCooldown);
        _canRoll = true;
    }

    private void HandleMovement()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        bool isMoving = input.sqrMagnitude > 0.01f;
        _isDashing = isMoving && Input.GetKey(KeyCode.LeftShift);

        if (_isDashing && _playerStats != null) _playerStats.ConsumeStamina(_dashStaminaCost * Time.deltaTime);

        float speed = _moveSpeed;
        if (_isDashing) speed *= _dashMultiplier;
        if (_playerStats != null) speed *= _playerStats.GetMoveSpeedMultiplier();

        Vector3 move = input * speed;
        _animator.SetFloat("Speed", move.magnitude);

        _controller.Move((move + _verticalVelocity) * Time.deltaTime);
    }

    private void HandleRollMovement() { }

    public void UpgradeSpeed(float amount) => _moveSpeed += amount;
    public float GetMoveSpeed() => CurrentMoveSpeed;
}