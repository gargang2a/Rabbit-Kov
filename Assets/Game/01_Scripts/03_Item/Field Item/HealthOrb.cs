using UnityEngine;

/// <summary>
/// 체력 회복 구슬 클래스
/// GlobalAudioManager와 연동되어 획득 시 사운드를 재생합니다.
/// </summary>
public class HealthOrb : MonoBehaviour
{
    [Header("Basic Settings")]
    [Tooltip("회복할 체력량")]
    [SerializeField] private float _healAmount = 20f;
    [SerializeField] private float _detectRange = 10f;

    [Header("Magnet Settings")]
    [SerializeField] private float _initialSmoothTime = 0.3f;
    [SerializeField] private float _finalSmoothTime = 0.01f;
    [SerializeField] private float _initialMaxSpeed = 10f;
    [SerializeField] private float _acceleration = 20f;

    [Header("Audio")]
    [SerializeField] private AudioClip _healSound;

    // 내부 상태 변수
    private Transform _playerTransform;
    private bool _isFollowing = false;
    private bool _isMagnetMode = false;
    private Vector3 _currentVelocity = Vector3.zero;

    private float _currentSmoothTime;
    private float _currentMaxSpeed;
    private ItemHighlighter _itemHighlighter;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _playerTransform = playerObj.transform;

        _itemHighlighter = GetComponent<ItemHighlighter>();
        _currentSmoothTime = _initialSmoothTime;
        _currentMaxSpeed = _initialMaxSpeed;
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        if (!_isFollowing)
        {
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            if (_isMagnetMode || distance < _detectRange) StartFollowing();
        }

        if (_isFollowing) MoveTowardsPlayer();
    }

    private void StartFollowing()
    {
        _isFollowing = true;
        if (_itemHighlighter != null) _itemHighlighter.enabled = false;
    }

    private void MoveTowardsPlayer()
    {
        _currentMaxSpeed += _acceleration * Time.deltaTime;
        _currentSmoothTime = Mathf.Lerp(_currentSmoothTime, _finalSmoothTime, Time.deltaTime);

        Vector3 targetPos = _playerTransform.position + Vector3.up * 1.0f;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, _currentSmoothTime, _currentMaxSpeed);
    }

    public void ActivateMagnet()
    {
        _isMagnetMode = true;
        if (!_isFollowing) StartFollowing();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) ApplyHeal(other.gameObject);
    }

    private void ApplyHeal(GameObject playerObj)
    {
        var player = playerObj.GetComponent<Player>();
        if (player != null)
        {
            player.Heal(_healAmount);
        }

        // ★ [GlobalAudioManager] 연동
        if (GlobalAudioManager.Instance != null && _healSound != null)
        {
            // 회복은 기분 좋은 소리이므로 약간의 피치 변화(0.1)를 줍니다.
            GlobalAudioManager.Instance.PlaySFX(_healSound, 0.1f);
        }

        Destroy(gameObject);
    }
}