using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("실제 빛을 내는 Light 컴포넌트")]
    [SerializeField] private Light _lightSource;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _toggleSound;

    [Header("Settings")]
    [Tooltip("게임 시작 시 손전등 켜짐 여부")]
    [SerializeField] private bool _isFlashlightOn = false;

    [Tooltip("입력 반복 입력을 방지하기 위한 쿨타임 (초)")]
    [SerializeField] private float _toggleCooldown = 0.2f;

    // 내부 로직용 변수
    private float _lastToggleTime = -999f; // 마지막으로 토글한 시간

    // 이벤트 (AI 시스템 연동용)
    public delegate void FlashlightEvent(bool isOn, Vector3 position);
    public static event FlashlightEvent OnFlashlightToggled;

    private void Awake()
    {
        InitializeFlashlight();
    }

    private void Update()
    {
        HandleInput();

        // [Escape from Duckov] 밤 시간대 Ghost 로직 등을 위해 켜져있다면 지속적인 위치 갱신이 필요할 수 있음
        if (_isFlashlightOn)
        {
            UpdateLightPositionLogic();
        }
    }

    private void InitializeFlashlight()
    {
        if (_lightSource == null)
            _lightSource = GetComponentInChildren<Light>();

        if (_lightSource == null)
        {
            Debug.LogError($"[FlashlightController] Light 컴포넌트가 {name}에 없습니다. Inspector를 확인하세요.");
            return;
        }

        // 그림자 깜빡임(Shadow Acne) 방지를 위한 코드 레벨 보정 (필요 시 활성화)
        // _lightSource.shadowBias = 0.05f; 
        // _lightSource.shadowNearPlane = 0.1f;

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = GetComponentInChildren<AudioSource>();
        }

        // 초기 상태 동기화
        ApplyLightState();
    }

    private void HandleInput()
    {
        // 쿨타임 체크: 너무 빠른 반복 입력을 막아 깜빡임 방지
        if (Time.time < _lastToggleTime + _toggleCooldown) return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleFlashlight();
            _lastToggleTime = Time.time;
        }
    }

    public void ToggleFlashlight()
    {
        _isFlashlightOn = !_isFlashlightOn;

        ApplyLightState();
        PlayToggleSound();

        // [Escape from Duckov] AI 시스템에 알림 (옵저버 패턴)
        // 적들이 이 이벤트를 구독하여 플레이어 위치로 Investigate 상태 전환 가능
        OnFlashlightToggled?.Invoke(_isFlashlightOn, transform.position);
    }

    private void ApplyLightState()
    {
        if (_lightSource != null)
        {
            _lightSource.enabled = _isFlashlightOn;
        }
    }

    private void PlayToggleSound()
    {
        if (_audioSource != null && _toggleSound != null)
        {
            _audioSource.PlayOneShot(_toggleSound);
        }
    }

    /// <summary>
    /// 손전등이 켜져있을 때 매 프레임 실행되는 로직
    /// </summary>
    private void UpdateLightPositionLogic()
    {
        // 예: 손전등이 벽 속에 파묻히지 않게 미세 조정하거나, 
        // 배터리를 소모하는 로직이 들어갈 자리
    }

    // 외부 접근용 프로퍼티
    public bool IsFlashlightOn => _isFlashlightOn;
}