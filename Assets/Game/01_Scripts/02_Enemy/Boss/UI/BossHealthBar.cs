using UnityEngine;
using UnityEngine.UI;

// [역할] 보스 체력바 UI - 화면 상단에 보스 체력 및 페이즈 표시
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
    
    private BossController _boss;
    private float _targetHealth = 1f;

    private void Awake()
    {
        gameObject.SetActive(false); // 시작 시 숨김
    }

    // 보스 연결 및 활성화
    public void Initialize(BossController boss)
    {
        _boss = boss;
        
        if (_bossNameText != null && boss.EnemyData != null)
        {
            _bossNameText.text = boss.EnemyData.enemyName;
        }
        
        if (boss.PhaseManager != null)
        {
            boss.PhaseManager.OnPhaseChanged += OnPhaseChanged;
        }
        
        UpdateHealthImmediate(1f);
        UpdatePhaseDisplay(1);
        
        gameObject.SetActive(true);
    }

    // 체력 업데이트 (부드럽게)
    public void UpdateHealth(float healthRatio)
    {
        _targetHealth = Mathf.Clamp01(healthRatio);
    }

    // 체력 즉시 업데이트
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
        if (_phaseText != null)
        {
            _phaseText.text = $"Phase {phase}";
        }
        
        // 페이즈별 체력바 색상
        if (_healthFill != null)
        {
            if (phase == 1)
            {
                _healthFill.color = _phase1Color;
            }
            else if (phase == 2)
            {
                _healthFill.color = _phase2Color;
            }
            else if (phase == 3)
            {
                _healthFill.color = _phase3Color;
            }
            else
            {
                _healthFill.color = _phase1Color;
            }
        }
    }

    // UI 숨기기
    public void Hide()
    {
        gameObject.SetActive(false);
        
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
