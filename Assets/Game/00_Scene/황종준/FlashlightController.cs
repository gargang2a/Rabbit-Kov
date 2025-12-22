using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Components")]
    // Inspector에서 직접 할당할 수 있도록 SerializeField 사용
    [Tooltip("실제 빛을 내는 Light 컴포넌트를 연결하세요.")]
    [SerializeField] private Light _lightSource;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _toggleSound;

    [Header("Settings")]
    [SerializeField] private bool _isFlashlightOn = false;

    private void Awake()
    {
        InitializeFlashlight();
    }

    private void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// 초기화 로직. 컴포넌트가 연결되지 않았을 경우 자동으로 찾습니다.
    /// </summary>
    private void InitializeFlashlight()
    {
        // Inspector에서 할당하지 않았다면, 현재 객체나 자식에서 찾음
        if (_lightSource == null)
        {
            _lightSource = GetComponentInChildren<Light>();
        }

        if (_lightSource == null)
        {
            Debug.LogError($"[FlashlightController] {_lightSource} 컴포넌트를 찾을 수 없습니다! Inspector에서 할당해주세요.");
            return;
        }

        if (_audioSource == null)
        {
            // 내 오브젝트에서 스피커를 찾아본다.
            _audioSource = GetComponent<AudioSource>();

            // 만약 내 몸에 없으면 자식들 중에서도 찾아본다.
            if (_audioSource == null)
            {
                _audioSource = GetComponentInChildren<AudioSource>();
            }
        }

        // 초기 상태 적용
        _lightSource.enabled = _isFlashlightOn;
    }

    /// <summary>
    /// 입력 처리 로직. 추후 Input System으로 교체 시 이 부분만 수정하면 됩니다.
    /// </summary>
    private void HandleInput()
    {
        // 장착 중일 때만 작동해야 한다면, 외부에서 이 스크립트를 활성/비활성화 하거나 조건을 추가해야 함
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleFlashlight();
        }
    }

    /// <summary>
    /// 손전등의 상태를 반전시킵니다.
    /// </summary>
    public void ToggleFlashlight()
    {
        if (_lightSource == null) return;

        _isFlashlightOn = !_isFlashlightOn;
        _lightSource.enabled = _isFlashlightOn;

        PlayToggleSound();

        // [확장 가능성] 여기에 AI 어그로 로직 추가 가능
        // 예: if (_isFlashlightOn) NotifyNearbyEnemies();
    }

    /// <summary>
    /// 스위치 조작 사운드 재생
    /// </summary>
    private void PlayToggleSound()
    {
        if (_audioSource != null && _toggleSound != null)
        {
            _audioSource.PlayOneShot(_toggleSound);
        }
    }

    // 외부(UI 등)에서 현재 상태를 확인하기 위한 프로퍼티
    public bool IsFlashlightOn => _isFlashlightOn;
}
