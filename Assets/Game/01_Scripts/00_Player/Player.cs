using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Player : MonoBehaviour, IDamageable
{
    // ==========================================
    // 1. 레벨 및 경험치
    // ==========================================
    [Header("Level & Exp")]
    [SerializeField] private int _level = 1;
    [SerializeField] private int _currentExp = 0;
    [SerializeField] private int _maxExp = 100;

    // 스텟 포인트 시스템
    [Header("Growth System")]
    [SerializeField] private int _statPoint = 0; // 남은 스텟 포인트
    [SerializeField] private float _spreadReduction = 0f; // 탄퍼짐 감소량 (수직손잡이 효과)

    public int Level => _level;
    public int Exp => _currentExp;
    public int MaxExp => _maxExp;
    public int StatPoint => _statPoint;
    public float SpreadReduction => _spreadReduction;

    // ==========================================
    // 2. 기본 스탯
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
    // 3. 상태 및 인벤토리
    // ==========================================
    [Space]
    [Header("Condition")]
    [SerializeField] private bool _isDead = false;
    public bool IsDead => _isDead;

    [Space]
    [Header("Inventory & Weight")]
    [SerializeField] private int _coin = 0;
    public int Coin => _coin;

    [Tooltip("최대 소지 무게")]
    [SerializeField] private float _maxWeight = 50f;
    [Tooltip("현재 소지 무게")]
    [SerializeField] private float _currentWeight = 0f;

    [Tooltip("몇 퍼센트부터 무거워질지 설정 (0.0 ~ 1.0)")]
    [Range(0f, 1f)][SerializeField] private float _overweightThreshold = 0.8f;

    public float MaxWeight => _maxWeight;
    public float CurrentWeight => _currentWeight;

    // 과적재 판정 (UI에서 사용)
    public bool IsOverweight => _currentWeight >= _maxWeight * _overweightThreshold;

    // ==========================================
    // 4. 이펙트 및 오디오
    // ==========================================
    [Space]
    [Header("Effects & Audio")]
    [SerializeField] private GameObject _levelUpVfxPrefab;
    [SerializeField] private AudioClip _levelUpSound;
    [SerializeField] private Vector3 _effectOffset = Vector3.zero;

    // ==========================================
    // 5. UI 참조
    // ==========================================
    [Space]
    [Header("UI References")]
    [SerializeField] private Image _hpBarImage;
    [SerializeField] private Image _staminaBarImage;
    [SerializeField] private Image _expBarCircular;
    [SerializeField] private RectTransform _hpBarRect;
    [SerializeField] private RectTransform _staminaBarRect;
    [SerializeField] private float _barWidthMultiplier = 2.0f;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _staminaText;
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _expText;

    // ==========================================
    // 6. 프로퍼티
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
    // 9. 전투 및 회복
    // ==========================================
    
    // IDamageable - 상세 버전 (넉백 강도 포함, 플레이어는 넉백 무시)
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection, float knockbackForce)
    {
        if (_isDead) return;
        int finalDamage = Mathf.Max(1, damage - _def);
        Hp -= finalDamage;
        // 플레이어 넉백은 별도 구현 가능 (현재 무시)
    }
    
    // IDamageable - 중간 버전
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection)
    {
        TakeDamage(damage, hitPoint, attackDirection, 0f);
    }
    
    // IDamageable - 간단 버전
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position, Vector3.zero, 0f);
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

    public void ConsumeStamina(float amount)
    {
        if (_isDead) return;
        Stamina -= amount;
    }

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
    // 10. 재화 및 성장
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

        // 레벨업 시 포인트 지급 (예: 5포인트)
        _statPoint += 5;

        Hp = MaxHp;
        Stamina = MaxStamina;

        Debug.Log($"Level Up! Current Level: {_level}, Point: {_statPoint}");
        PlayLevelUpEffect();
    }

    private void PlayLevelUpEffect()
    {
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
    // 11. 업그레이드 및 아이템 획득
    // ==========================================

    // 공격력 강화 (UI 버튼에서 호출)
    public bool TryUpgradeAtk()
    {
        if (_statPoint > 0)
        {
            _atk += 1;
            _statPoint--;
            return true;
        }
        return false;
    }

    // 체력 강화
    public bool TryUpgradeHp()
    {
        if (_statPoint > 0)
        {
            MaxHp += 10f;
            Hp = MaxHp;
            UpdateUI();
            _statPoint--;
            return true;
        }
        return false;
    }

    // 스태미너 강화
    public bool TryUpgradeStamina()
    {
        if (_statPoint > 0)
        {
            MaxStamina += 10f;
            Stamina = MaxStamina;
            UpdateUI();
            _statPoint--;
            return true;
        }
        return false;
    }

    // 이동속도 강화
    public bool TryUpgradeSpeed()
    {
        if (_statPoint > 0)
        {
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.UpgradeSpeed(0.5f);
                _statPoint--;
                return true;
            }
        }
        return false;
    }

    public void UpgradeAtk(int amount)
    {
        _atk += amount;
    }

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

    // 수직 손잡이 획득 (탄퍼짐 감소)
    public void AcquireVerticalGrip(float amount)
    {
        _spreadReduction += amount;
        Debug.Log($"수직 손잡이 장착! 탄퍼짐 {_spreadReduction} 감소");
    }

    // ==========================================
    // 12. 무게 시스템
    // ==========================================
    public float GetMoveSpeedMultiplier()
    {
        if (IsOverweight) return 0.5f;
        return 1.0f;
    }

    public void UpdateWeight(float newWeight)
    {
        _currentWeight = newWeight;
    }

    public void ExpandMaxWeight(float amount)
    {
        _maxWeight += amount;
    }
}