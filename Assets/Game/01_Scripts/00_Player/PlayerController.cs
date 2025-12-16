using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // === Inspector Settings ===
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _dashMultiplier = 1.5f;
    [SerializeField] private float _rotationSpeed = 720f;
    [SerializeField] private float _gravity = -30f;

    [Tooltip("달리기 스태미너 소모량 (초당)")]
    [SerializeField] private float _dashStaminaCost = 15f;
    [Tooltip("스태미너가 바닥났을 때, 다시 달리기 위해 필요한 최소 스태미너 양")]
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

    // 달리기 잠금 상태 (지침)
    [SerializeField] private bool _isRunLocked = false;

    private Vector3 _rollVelocity;
    private Vector3 _verticalVelocity;

    // === References ===
    private CharacterController _controller;
    private Rigidbody _rb;
    private Animator _animator;
    private Camera _mainCamera;

    private PlayerAttack _playerAttack;
    private Player _playerStats;

    // === Public Properties ===
    public bool IsRolling => _isRolling;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _playerAttack = GetComponent<PlayerAttack>();
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
        // 1. 사망 체크
        if (_playerStats != null && _playerStats.isDead)
        {
            _animator.SetFloat("Speed", 0f);
            return;
        }

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
        if (_controller.isGrounded)
        {
            _isGrounded = true;
            _verticalVelocity.y = -2f;
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
        float h = 0f;
        float v = 0f;

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
            {
                _isRunLocked = false;
            }
        }
        if (isMoving && isShiftHeld && !_isRunLocked)
        {
            if (_playerStats != null && _playerStats.Stamina > 0)
            {
                _isDashing = true;
                _playerStats.Stamina -= _dashStaminaCost * Time.deltaTime;

                if (_playerStats.Stamina <= 0)
                {
                    _playerStats.Stamina = 0;
                    _isRunLocked = true;
                    _isDashing = false;
                }
            }
            else
            {
                _isDashing = false;
            }
        }
        else
        {
            _isDashing = false;
        }
        float currentSpeed = _moveSpeed;
        if (_isDashing) currentSpeed *= _dashMultiplier;

        Vector3 finalMove = _verticalVelocity;
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

    private void HandleRollInput()
    {
        if (Input.GetKeyDown(_rollKey) && _canRoll)
        {
            // ★ [공격 중 구르기 방지]
            if (_playerAttack != null && _playerAttack.IsAttacking) return;

            // 스태미너 체크 및 소모
            if (_playerStats != null && _playerStats.UseStamina(_rollStaminaCost))
            {
                StartRoll();
            }
        }
    }

    private void StartRoll()
    {
        _canRoll = false;
        _isDashing = false;
        _isRolling = true;

        float h = 0f;
        float v = 0f;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        Vector3 rollDir = (inputDir.magnitude >= 0.1f) ? inputDir : transform.forward;

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