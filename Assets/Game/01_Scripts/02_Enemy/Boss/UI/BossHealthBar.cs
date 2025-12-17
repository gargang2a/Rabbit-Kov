using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [Role] 보스 체력바 UI
/// 화면 상단에 보스 체력 및 페이즈 표시
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Image _healthFill;
    [SerializeField] private Text _bossNameText;
    [SerializeField] private Text _phaseText;
    
    [Header("페이즈 색상")]
    [SerializeField] private Color _phase1Color = Color.green;
    [SerializeField] private Color _phase2Color = Color.yellow;
    [SerializeField] private Color _phase3Color = Color.red;
    
    [Header("애니메이션")]
    [SerializeField] private float _smoothSpeed = 5f;
    
    // 참조
    private BossController _boss;
    private float _targetHealth = 1f;

    private void Awake()
    {
        // 시작 시 숨김
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 보스 연결 및 UI 활성화
    /// </summary>
    public void Initialize(BossController boss)
    {
        _boss = boss;
        
        // 이름 설정
        if (_bossNameText != null && boss.EnemyData != null)
        {
            _bossNameText.text = boss.EnemyData.enemyName;
        }
        
        // 페이즈 이벤트 구독
        if (boss.PhaseManager != null)
        {
            boss.PhaseManager.OnPhaseChanged += OnPhaseChanged;
        }
        
        // 초기 상태
        UpdateHealthImmediate(1f);
        UpdatePhaseDisplay(1);
        
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 체력 업데이트 (부드러운 애니메이션)
    /// </summary>
    public void UpdateHealth(float healthRatio)
    {
        _targetHealth = Mathf.Clamp01(healthRatio);
    }

    /// <summary>
    /// 체력 즉시 업데이트 (애니메이션 없음)
    /// </summary>
    public void UpdateHealthImmediate(float healthRatio)
    {
        _targetHealth = Mathf.Clamp01(healthRatio);
        if (_healthSlider != null)
        {
            _healthSlider.value = _targetHealth;
        }
    }

    private void Update()
    {
        if (_healthSlider == null) return;
        
        // 부드러운 체력바 애니메이션
        _healthSlider.value = Mathf.Lerp(_healthSlider.value, _targetHealth, _smoothSpeed * Time.deltaTime);
    }

    private void OnPhaseChanged(int newPhase)
    {
        UpdatePhaseDisplay(newPhase);
    }

    private void UpdatePhaseDisplay(int phase)
    {
        // 페이즈 텍스트
        if (_phaseText != null)
        {
            _phaseText.text = $"Phase {phase}";
        }
        
        // 체력바 색상 변경
        if (_healthFill != null)
        {
            _healthFill.color = phase switch
            {
                1 => _phase1Color,
                2 => _phase2Color,
                3 => _phase3Color,
                _ => _phase1Color
            };
        }
    }

    /// <summary>
    /// UI 숨기기 (보스 사망 시)
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        
        // 이벤트 해제
        if (_boss?.PhaseManager != null)
        {
            _boss.PhaseManager.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnDestroy()
    {
        if (_boss?.PhaseManager != null)
        {
            _boss.PhaseManager.OnPhaseChanged -= OnPhaseChanged;
        }
    }
}
