using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어의 스탯, 상태, UI 갱신, 이펙트 및 자원 관리를 담당하는 핵심 클래스
/// </summary>
public class Player : MonoBehaviour, IDamageable
{
    // ==========================================
    // 1. 레벨 및 경험치 (Level & Exp)
    // ==========================================
    [Header("Level & Exp")]
    [SerializeField] private int _level = 1;
    [SerializeField] private int _currentExp = 0;
    [SerializeField] private int _maxExp = 100;

    public int Level => _level;
    public int Exp => _currentExp;
    public int MaxExp => _maxExp;

    // ==========================================
    // 2. 기본 스탯 (Base Stats)
    // ==========================================
    [Header("Player Stats")]
    [SerializeField] private float _currentHp;
    [SerializeField] private float _currentStamina;
    [SerializeField] private float _staminaRegenSpeed = 20f;

    public float MaxHp { get; private set; } = 100f;
    public float MaxStamina { get; private set; } = 100f;

    [Header("Battle Stats")]
    [SerializeField] private int _atk;
    [SerializeField] private int _def;
    [SerializeField] private int _shield;

    public int Atk => _atk;
    public int Def => _def;
    public int Shield => _shield;

    // ==========================================
    // 3. 상태 및 인벤토리 (State & Inventory)
    // ==========================================
    [Space]
    [Header("Condition")]
    [SerializeField] private bool _isDead = false;
    public bool IsDead => _isDead;

    [Space]
    [Header("Inventory")]
    [SerializeField] private int _coin = 0;
    public int Coin => _coin;

    // ==========================================
    // 4. 이펙트 및 오디오 (Effects & Audio)
    // ==========================================
    [Space]
    [Header("Effects & Audio")]
    [Tooltip("레벨업 시 재생할 파티클 프리팹")]
    [SerializeField] private GameObject _levelUpVfxPrefab;

    [Tooltip("레벨업 시 재생할 사운드")]
    [SerializeField] private AudioClip _levelUpSound;

    [Tooltip("이펙트가 생성될 위치 오프셋")]
    [SerializeField] private Vector3 _effectOffset = Vector3.zero;

    // ==========================================
    // 5. UI 참조 (UI References)
    // ==========================================
    [Space]
    [Header("UI References")]
    [SerializeField] private Image _hpBarImage;
    [SerializeField] private Image _staminaBarImage;
    [SerializeField] private Image _expBarCircular;

    [Header("UI RectTransforms")]
    [SerializeField] private RectTransform _hpBarRect;
    [SerializeField] private RectTransform _staminaBarRect;

    [Header("UI Settings")]
    [SerializeField] private float _barWidthMultiplier = 2.0f;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _staminaText;
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _expText;

    // ==========================================
    // 6. 프로퍼티 (Properties)
    // ==========================================
    public float Hp
    {
        get => _currentHp;
        private set
        {
            _currentHp = Mathf.Clamp(value, 0, MaxHp);
            UpdateUI();
            if (_currentHp <= 0 && !_isDead) { _currentHp = 0; Die(); }
        }
    }

    public float Stamina
    {
        get => _currentStamina;
        private set
        {
            _currentStamina = Mathf.Clamp(value, 0, MaxStamina);
            UpdateUI();
        }
    }

    // ==========================================
    // 7. 유니티 라이프사이클
    // ==========================================
    private void Awake()
    {
        Hp = MaxHp;
        Stamina = MaxStamina;
        _isDead = false;
        UpdateUI();
    }

    private void Update()
    {
        if (_isDead) return;

        // 스태미나 자동 회복
        if (Stamina < MaxStamina)
        {
            Stamina += _staminaRegenSpeed * Time.deltaTime;
        }
    }

    // ==========================================
    // 8. UI 업데이트
    // ==========================================
    private void UpdateUI()
    {
        if (_hpBarImage != null) _hpBarImage.fillAmount = _currentHp / MaxHp;
        if (_staminaBarImage != null) _staminaBarImage.fillAmount = _currentStamina / MaxStamina;
        if (_expBarCircular != null) _expBarCircular.fillAmount = (float)_currentExp / (float)_maxExp;

        if (_hpBarRect != null)
            _hpBarRect.sizeDelta = new Vector2(MaxHp * _barWidthMultiplier, _hpBarRect.sizeDelta.y);

        if (_staminaBarRect != null)
            _staminaBarRect.sizeDelta = new Vector2(MaxStamina * _barWidthMultiplier, _staminaBarRect.sizeDelta.y);

        if (_hpText != null) _hpText.text = $"{_currentHp:F0} / {MaxHp:F0}";
        if (_staminaText != null) _staminaText.text = $"{_currentStamina:F0} / {MaxStamina:F0}";
        if (_coinText != null) _coinText.text = $"{_coin}";
        if (_levelText != null) _levelText.text = $"Lv.{_level}";
        if (_expText != null) _expText.text = $"{_currentExp} / {_maxExp}";
    }

    // ==========================================
    // 9. 전투 및 회복 (Combat & Recovery)
    // ==========================================
    public void TakeDamage(int damage)
    {
        if (_isDead) return;
        int finalDamage = Mathf.Max(1, damage - _def);
        Hp -= finalDamage;
    }

    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection)
    {
        if (_isDead) return;
        int finalDamage = Mathf.Max(1, damage - _def);
        Hp -= finalDamage;
    }

    public void Heal(float amount)
    {
        if (_isDead) return;
        Hp += amount;
    }

    public void RestoreStamina(float amount)
    {
        if (_isDead) return;
        Stamina += amount;
    }

    /// <summary>
    /// ★ [추가됨] 지속적인 스태미나 소모 (달리기 등)
    /// PlayerController에서 호출합니다.
    /// </summary>
    public void ConsumeStamina(float amount)
    {
        if (_isDead) return;
        Stamina -= amount; // 프로퍼티 set에서 Clamp 처리됨
    }

    /// <summary>
    /// 즉발성 스태미나 소모 (구르기, 공격 등)
    /// </summary>
    /// <returns>성공 여부</returns>
    public bool UseStamina(int amount)
    {
        if (Stamina >= amount)
        {
            Stamina -= amount;
            return true;
        }
        return false;
    }

    private void Die()
    {
        _isDead = true;
        Debug.Log("Player Died.");
    }

    // ==========================================
    // 10. 재화 및 성장 (Level Up Logic)
    // ==========================================
    public void GainCoin(int amount)
    {
        _coin += amount;
        UpdateUI();
    }

    public bool UseCoin(int amount)
    {
        if (_coin >= amount)
        {
            _coin -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    public void GainExp(int amount)
    {
        _currentExp += amount;
        while (_currentExp >= _maxExp)
        {
            LevelUp();
        }
        UpdateUI();
    }

    private void LevelUp()
    {
        _currentExp -= _maxExp;
        _level++;
        _maxExp += 50;

        Hp = MaxHp;
        Stamina = MaxStamina;

        Debug.Log($"Level Up! Current Level: {_level}");
        PlayLevelUpEffect();
    }

    private void PlayLevelUpEffect()
    {
        if (SoundManager.instance != null && _levelUpSound != null)
        {
            SoundManager.instance.PlaySFX(_levelUpSound);
        }

        if (_levelUpVfxPrefab != null)
        {
            GameObject vfx = Instantiate(_levelUpVfxPrefab, transform.position + _effectOffset, Quaternion.identity);
            vfx.transform.SetParent(transform);

            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            float duration = (ps != null) ? ps.main.duration : 2.0f;
            Destroy(vfx, duration + 0.5f);
        }
    }

    // ==========================================
    // 11. 업그레이드
    // ==========================================
    public void UpgradeAtk(int amount) { _atk += amount; }

    public void UpgradeHp(float amount)
    {
        MaxHp += amount;
        Hp = MaxHp;
        UpdateUI();
    }

    public void UpgradeStamina(float amount)
    {
        MaxStamina += amount;
        Stamina = MaxStamina;
        UpdateUI();
    }
}