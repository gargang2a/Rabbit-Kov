using UnityEngine;

public class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private AudioSource _sfxSource;

    [Header("High Frequency Settings (Exp)")]
    [Tooltip("최소 재생 간격 (이 시간보다 빠르게 들어오면 소리 씹힘)")]
    [SerializeField] private float _minExpInterval = 0.03f;
    [Tooltip("연속 획득 시 피치 상승량")]
    [SerializeField] private float _pitchStep = 0.05f;
    [Tooltip("최대 피치 제한")]
    [SerializeField] private float _maxPitch = 2.0f;
    [Tooltip("콤보 초기화 시간 (이 시간 동안 안 먹으면 피치 원상복구)")]
    [SerializeField] private float _comboResetTime = 1.0f;

    // 내부 변수
    private float _lastExpPlayTime;
    private int _comboCount;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (_sfxSource == null) _sfxSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 일반적인 단발성 효과음 (UI, 단일 아이템 등)
    /// </summary>
    public void PlaySFX(AudioClip clip, float pitchVariety = 0.1f)
    {
        if (clip == null) return;
        _sfxSource.pitch = 1f + Random.Range(-pitchVariety, pitchVariety);
        _sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// [New] 대량으로 획득하는 경험치 전용 메서드
    /// 스로틀링(Throttling)과 라이징 피치(Rising Pitch) 적용
    /// </summary>
    public void PlayExpSFX(AudioClip clip)
    {
        if (clip == null) return;

        float currentTime = Time.time;

        // 1. 스로틀링: 너무 짧은 시간(0.03초) 내에 재호출되면 소리 재생 스킵 (성능 및 귀 보호)
        if (currentTime - _lastExpPlayTime < _minExpInterval)
        {
            return;
        }

        // 2. 콤보 리셋 체크: 마지막 획득 후 시간이 오래 지났으면 피치 초기화
        if (currentTime - _lastExpPlayTime > _comboResetTime)
        {
            _comboCount = 0;
        }

        _lastExpPlayTime = currentTime;

        // 3. 피치 계산: 기본 1.0 + (콤보 * 스텝) -> 최대값 제한
        float targetPitch = 1.0f + (_comboCount * _pitchStep);
        _sfxSource.pitch = Mathf.Clamp(targetPitch, 1.0f, _maxPitch);

        // 4. 재생 및 콤보 증가
        _sfxSource.PlayOneShot(clip);
        _comboCount++;
    }
}