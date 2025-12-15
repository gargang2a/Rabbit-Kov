using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // === Inspector Settings ===
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 8f; // (12는 너무 빠를 수 있어 8로 조정, 원하면 변경)
    [SerializeField] private float _dashMultiplier = 1.5f;
    [SerializeField] private float _rotationSpeed = 720f;
    [SerializeField] private float _gravity = -30f;

    // ★ [복구됨] 달리기 스태미너 소모량
    [SerializeField] private float _dashStaminaCost = 15f;
    // ★ [복구됨] 지침 상태 해제 기준 (스태미너가 이만큼 차야 다시 달림)
    [SerializeField] private float _runRecoveryThreshold = 20f;

    [Header("Roll Settings")]
    [SerializeField] private KeyCode _rollKey = KeyCode.Space;
    [SerializeField] private float _rollDuration = 0.5f;
    [SerializeField] private float _rollCooldown = 0.3f;
    [SerializeField] private float _rollDistance = 12f;
    [SerializeField] private int _rollStaminaCost = 25; // ★ [복구됨] 구르기 소모량

    [Header("Dead Zone")]
    [SerializeField] private float _minRotationDistance = 1.0f;

    [Header("Internal State")]
    [SerializeField] private bool _canRoll = true;
    [SerializeField] private bool _isRolling = false;
    [SerializeField] private bool _isDashing = false;
    [SerializeField] private bool _isGrounded;

    // ★ [복구됨] 달리기 잠금 상태 (지침)
    [SerializeField] private bool _isRunLocked = false;

    private Vector3 _rollVelocity;
    private Vector3 _verticalVelocity;

    // === References ===
    private CharacterController _controller;
    private Rigidbody _rb;
    private Animator _animator;
    private Camera _mainCamera;

    private PlayerAttack _playerAttack;
    private Player _playerStats; // ★ [복구됨] Player 참조 변수

    // === Public Properties ===
    public bool IsRolling => _isRolling;

    private void Reset()
    {
        _moveSpeed = 8f;
    }

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _playerAttack = GetComponent<PlayerAttack>();
        _playerStats = GetComponent<Player>(); // ★ [복구됨] 스크립트 가져오기
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
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 moveDir = new Vector3(h, 0f, v).normalized;
        bool isMoving = moveDir.magnitude >= 0.1f;
        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift);

        // ★ [복구됨] 달리기 잠금 해제 체크
        if (_isRunLocked)
        {
            if (_playerStats != null && _playerStats.Stamina >= _runRecoveryThreshold)
            {
                _isRunLocked = false;
            }
        }

        // ★ [복구됨] 달리기 로직 (스태미너 소모 포함)
        if (isMoving && isShiftHeld && !_isRunLocked)
        {
            if (_playerStats != null && _playerStats.Stamina > 0)
            {
                _isDashing = true;

                // 실제 스태미너 감소
                _playerStats.Stamina -= _dashStaminaCost * Time.deltaTime;

                // 0이 되면 잠금 걸기
                if (_playerStats.Stamina <= 0)
                {
                    _playerStats.Stamina = 0;
                    _isRunLocked = true; // 지침 상태!
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

        // 속도 적용
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
            if (_playerAttack != null && _playerAttack.IsAttacking) return;

            // ★ [복구됨] 구르기 스태미너 체크
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

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
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

    // [유지됨] 이동속도 업그레이드 함수 (UI에서 호출)
    public void UpgradeSpeed(float amount)
    {
        _moveSpeed += amount;
        Debug.Log("이동속도 증가! 현재: " + _moveSpeed);
    }

    // [유지됨] UI 표시용
    public float GetMoveSpeed()
    {
        return _moveSpeed;
    }
}