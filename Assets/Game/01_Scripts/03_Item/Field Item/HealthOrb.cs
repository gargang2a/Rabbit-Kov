using UnityEngine;

/// <summary>
/// 체력 회복 구슬 클래스
/// ExpOrb와 동일한 물리/추적 로직을 사용하여 게임의 조작감(Game Feel)을 통일했습니다.
/// </summary>
public class HealthOrb : MonoBehaviour
{
    [Header("Basic Settings")]
    [Tooltip("회복할 체력량")]
    [SerializeField] private float _healAmount = 20f;
    [SerializeField] private float _detectRange = 10f;

    [Header("Magnet Settings")]
    [Tooltip("초기 반응 속도 (낮을수록 빠름)")]
    [SerializeField] private float _initialSmoothTime = 0.3f;

    [Tooltip("최종 반응 속도 (추적 후반부의 빠릿함)")]
    [SerializeField] private float _finalSmoothTime = 0.01f;

    [Tooltip("초기 최대 속도")]
    [SerializeField] private float _initialMaxSpeed = 10f;

    [Tooltip("초당 속도 증가량 (가속도)")]
    [SerializeField] private float _acceleration = 20f;

    [Header("Audio")]
    [SerializeField] private AudioClip _healSound;

    // 내부 상태 변수
    private Transform _playerTransform;
    private bool _isFollowing = false;
    private bool _isMagnetMode = false;
    private Vector3 _currentVelocity = Vector3.zero;

    // 가속 로직 변수
    private float _currentSmoothTime;
    private float _currentMaxSpeed;

    // 시각 효과 제어용 (기존 ExpOrb와 동일하게 처리)
    private ItemHighlighter _itemHighlighter;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }

        _itemHighlighter = GetComponent<ItemHighlighter>();

        // 변수 초기화
        _currentSmoothTime = _initialSmoothTime;
        _currentMaxSpeed = _initialMaxSpeed;
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        // 1. 추적 상태가 아닐 때: 탐지
        if (!_isFollowing)
        {
            float distance = Vector3.Distance(transform.position, _playerTransform.position);

            if (_isMagnetMode || distance < _detectRange)
            {
                StartFollowing();
            }
        }

        // 2. 추적 상태일 때: 이동
        if (_isFollowing)
        {
            MoveTowardsPlayer();
        }
    }

    private void StartFollowing()
    {
        _isFollowing = true;
        if (_itemHighlighter != null)
        {
            _itemHighlighter.enabled = false;
        }
    }

    private void MoveTowardsPlayer()
    {
        // 가속 로직: 시간이 지날수록 빨라짐
        _currentMaxSpeed += _acceleration * Time.deltaTime;
        _currentSmoothTime = Mathf.Lerp(_currentSmoothTime, _finalSmoothTime, Time.deltaTime);

        // 목표 지점 (플레이어 위치)
        Vector3 targetPos = _playerTransform.position + Vector3.up * 1.0f;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref _currentVelocity,
            _currentSmoothTime,
            _currentMaxSpeed
        );
    }

    public void ActivateMagnet()
    {
        _isMagnetMode = true;
        if (!_isFollowing)
        {
            StartFollowing();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyHeal(other.gameObject);
        }
    }

    private void ApplyHeal(GameObject playerObj)
    {
        var player = playerObj.GetComponent<Player>();

        if (player != null)
        {
            // ★ Player 스크립트에 추가된 Heal 메서드 호출
            player.Heal(_healAmount);
        }

        if (SoundManager.instance != null && _healSound != null)
        {
            SoundManager.instance.PlaySFX(_healSound);
        }

        Destroy(gameObject);
    }
}