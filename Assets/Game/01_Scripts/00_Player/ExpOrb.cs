using UnityEngine;

/// <summary>
/// 경험치 구슬의 로직(탐지, 이동, 획득)을 담당하는 클래스입니다.
/// 플레이어 추적 시 시간이 지날수록 가속도가 붙어 확실하게 흡수되도록 개선되었습니다.
/// </summary>
public class ExpOrb : MonoBehaviour
{
    [Header("Basic Settings")]
    [SerializeField] private int _expAmount = 10;
    [SerializeField] private float _detectRange = 5f;

    [Header("Magnet Settings")]
    [Tooltip("초기 반응 속도 (낮을수록 빠름)")]
    [SerializeField] private float _initialSmoothTime = 0.3f;

    [Tooltip("최종 반응 속도 (추적 후반부의 빠릿함)")]
    [SerializeField] private float _finalSmoothTime = 0.05f;

    [Tooltip("초기 최대 속도")]
    [SerializeField] private float _initialMaxSpeed = 10f;

    [Tooltip("초당 속도 증가량 (가속도)")]
    [SerializeField] private float _acceleration = 20f;

    [Header("Audio")]
    [SerializeField] private AudioClip _expSound;

    // 내부 상태 변수
    private Transform _playerTransform;
    private bool _isFollowing = false;
    private bool _isMagnetMode = false;
    private Vector3 _currentVelocity = Vector3.zero;

    // 가속 로직을 위한 동적 변수
    private float _currentSmoothTime;
    private float _currentMaxSpeed;

    // ★ 충돌 해결을 위한 참조 변수
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

        // 1. 추적 상태가 아닐 때: 탐지 로직 수행
        if (!_isFollowing)
        {
            float distance = Vector3.Distance(transform.position, _playerTransform.position);

            if (_isMagnetMode || distance < _detectRange)
            {
                StartFollowing();
            }
        }

        // 2. 추적 상태일 때: 이동 로직 수행
        if (_isFollowing)
        {
            MoveTowardsPlayer();
        }
    }

    private void StartFollowing()
    {
        _isFollowing = true;

        // 시각 효과 끄기
        if (_itemHighlighter != null)
        {
            _itemHighlighter.enabled = false;
        }
    }

    private void MoveTowardsPlayer()
    {
        // ★ 핵심 로직: 시간 경과에 따른 가속 처리
        // 1. 최대 속도를 매 프레임 증가시킵니다 (플레이어가 도망쳐도 결국 따라잡음).
        _currentMaxSpeed += _acceleration * Time.deltaTime;

        // 2. 반응 속도(SmoothTime)를 점점 줄여서 더 즉각적으로 따라붙게 만듭니다.
        _currentSmoothTime = Mathf.Lerp(_currentSmoothTime, _finalSmoothTime, Time.deltaTime);

        // 목표 지점 설정 (플레이어 허리춤)
        Vector3 targetPos = _playerTransform.position + Vector3.up * 1.0f;

        // SmoothDamp에 동적으로 변하는 속도 변수 적용
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
            Collect(other.gameObject);
        }
    }

    private void Collect(GameObject playerObj)
    {
        var player = playerObj.GetComponent<Player>();
        if (player != null)
        {
            player.GainExp(_expAmount);
        }

        if (SoundManager.instance != null && _expSound != null)
        {
            SoundManager.instance.PlaySFX(_expSound);
        }

        Destroy(gameObject);
    }
}