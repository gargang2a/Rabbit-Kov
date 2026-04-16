using UnityEngine;

public abstract class ParentOrb : MonoBehaviour
{
    [SerializeField] private float _detectRange = 10f;
    [SerializeField] private float _initialSmoothTime = 0.3f;
    [SerializeField] private float _finalSmoothTime = 0.01f;
    [SerializeField] private float _initialMaxSpeed = 10f;
    [SerializeField] private float _acceleration = 20f;

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

    public void ActivateMagnet()
    {
        _isMagnetMode = true;
        if (!_isFollowing) StartFollowing();
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            
            if (player != null)
            {
                ApplyEffect(player);
                Destroy(gameObject);
            }
        }
    }

    protected abstract void ApplyEffect(Player player);
}


