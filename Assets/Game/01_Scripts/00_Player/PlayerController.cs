using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // === Settings ===
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 16f;
    [SerializeField] private float _dashMultiplier = 1.5f;
    [SerializeField] private float _rotationSpeed = 720f;
    [SerializeField] private float _gravity = -30f;
    [SerializeField] private float _terminalVelocity = -50f;

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
        HandleRotation();
        HandleRollInput();
        HandleImpact();

        if (!_isRolling) HandleMovement();
        else HandleRollMovement();
    }

    // ==========================================
    // ★ [Fix] 외부 피격 함수들 (에러 해결 핵심)
    // ==========================================

    // 1. 넉백 (벡터 버전)
    public void ApplyKnockback(Vector3 force)
    {
        _impactVelocity += force;
    }

    // 2. 띄우기 (벡터 버전 - 기존)
    public void ApplyLaunch(Vector3 launchForce)
    {
        _impactVelocity += launchForce;
        if (launchForce.y > 0)
        {
            _isGrounded = false;
            _verticalVelocity.y = 0;
        }
    }

    // ★ [New] 띄우기 (Float 버전 - 에러 해결용)
    // 보스가 숫자만 보내면 "위쪽 방향"으로 자동 변환해서 처리합니다.
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

    // ==========================================
    // 이동 로직
    // ==========================================
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

    private void HandleRotation()
    {
        if (_isRolling) return;
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, transform.position);
        if (ground.Raycast(ray, out float enter))
        {
            Vector3 target = ray.GetPoint(enter);
            Vector3 dir = target - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.1f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), _rotationSpeed * Time.deltaTime);
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