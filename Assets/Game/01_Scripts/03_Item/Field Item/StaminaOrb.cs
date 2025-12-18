using UnityEngine;

/// <summary>
/// 스태미나 회복 구슬 클래스
/// HealthOrb와 동일한 물리/추적 로직을 사용하여 일관된 습득 경험을 제공합니다.
/// </summary>
public class StaminaOrb : MonoBehaviour
{
    [Header("Basic Settings")]
    [Tooltip("회복할 스태미나 양")]
    [SerializeField] private float _restoreAmount = 30f; // 체력보다 스태미나는 보통 수치가 넉넉하므로 기본값 상향
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
    [SerializeField] private AudioClip _restoreSound;

    // 내부 상태 변수
    private Transform _playerTransform;
    private bool _isFollowing = false;
    private bool _isMagnetMode = false;
    private Vector3 _currentVelocity = Vector3.zero;

    // 가속 로직 변수
    private float _currentSmoothTime;
    private float _currentMaxSpeed;

    // 시각 효과 제어용
    private ItemHighlighter _itemHighlighter;

    private void Start()
    {
        // 태그를 이용한 플레이어 검색 (싱글톤 패턴이 있다면 Instance 접근 권장)
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

        // 추적 시작 시 하이라이트 효과 끄기 (최적화 및 시각적 깔끔함)
        if (_itemHighlighter != null)
        {
            _itemHighlighter.enabled = false;
        }
    }

    private void MoveTowardsPlayer()
    {
        // 가속 로직: 시간이 지날수록 빨라짐 (Game Feel: 빨려 들어가는 느낌)
        _currentMaxSpeed += _acceleration * Time.deltaTime;
        _currentSmoothTime = Mathf.Lerp(_currentSmoothTime, _finalSmoothTime, Time.deltaTime);

        // 목표 지점 (플레이어의 중심 혹은 약간 위쪽)
        Vector3 targetPos = _playerTransform.position + Vector3.up * 1.0f;

        // SmoothDamp를 사용하여 부드럽게 이동
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref _currentVelocity,
            _currentSmoothTime,
            _currentMaxSpeed
        );
    }

    /// <summary>
    /// 외부(예: 자석 아이템 습득)에서 강제로 끌어당길 때 호출
    /// </summary>
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
        // 플레이어와 충돌 시 효과 적용
        if (other.CompareTag("Player"))
        {
            ApplyStamina(other.gameObject);
        }
    }

    private void ApplyStamina(GameObject playerObj)
    {
        var player = playerObj.GetComponent<Player>();

        if (player != null)
        {
            // Player 스크립트에 추가할 RestoreStamina 메서드 호출
            player.RestoreStamina(_restoreAmount);
        }

        if (SoundManager.instance != null && _restoreSound != null)
        {
            SoundManager.instance.PlaySFX(_restoreSound);
        }

        Destroy(gameObject);
    }
}