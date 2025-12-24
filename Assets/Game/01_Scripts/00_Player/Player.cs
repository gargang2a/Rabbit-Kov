using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour, IDamageable
{
    // ★ [이벤트] 플레이어 사망 시퀀스 종료 알림
    public static event Action OnPlayerDeathSequenceCompleted;

    // ==========================================
    // 1. 레벨 및 경험치
    // ==========================================
    [Header("Level & Exp")]
    [SerializeField] private int _level = 1;
    [SerializeField] private int _currentExp = 0;
    [SerializeField] private int _maxExp = 100;

    [Header("Growth")]
    [SerializeField] private int _statPoint = 0;
    [SerializeField] private float _spreadReduction = 0f;

    public int Level => _level;
    public int CurrentExp => _currentExp;
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
    [SerializeField] private int _atk = 10;
    [SerializeField] private int _def;
    [SerializeField] private int _shield;

    public int Atk => _atk;
    public int BaseAttack => _atk;
    public int Def => _def;
    public int Shield => _shield;

    // 캐싱된 컨트롤러
    private PlayerController _cachedController;
    public float MoveSpeed
    {
        get
        {
            if (_cachedController == null) _cachedController = GetComponent<PlayerController>();
            return _cachedController != null ? _cachedController.CurrentMoveSpeed : 0f;
        }
    }

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

    [SerializeField] private int _killCount = 0;
    public int KillCount => _killCount;

    [SerializeField] private float _maxWeight = 50f;
    [SerializeField] private float _currentWeight = 0f;
    [Range(0f, 1f)][SerializeField] private float _overweightThreshold = 0.8f;

    private bool _wasOverweight = false;

    public float MaxWeight
    {
        get => _maxWeight;
        set => _maxWeight = value;
    }
    public float CurrentWeight => _currentWeight;
    public bool IsOverweight => _currentWeight >= _maxWeight * _overweightThreshold;

    // ==========================================
    // 4. 이펙트 및 오디오
    // ==========================================
    [Space]
    [Header("Effects & Audio")]
    [SerializeField] private GameObject _levelUpVfxPrefab;
    [SerializeField] private AudioClip _levelUpSound;
    [SerializeField] private Vector3 _effectOffset = Vector3.zero;

    [Header("Damage Feedback")]
    [SerializeField] private AudioClip _hurtSound;          // 신음 소리
    [SerializeField] private Color _damageFlashColor = new Color(1f, 0.3f, 0.3f, 1f); // 피격 시 붉은색
    [SerializeField] private float _flashDuration = 0.05f;  // 깜빡임 지속 시간

    [Header("Death Settings")]
    [SerializeField] private GameObject _deathVfxPrefab;       // 기존: 영혼 승천 이펙트
    [SerializeField] private GameObject _deathImpactVfxPrefab; // ★ [New] 사망 순간 터지는 이펙트 (폭발/피)
    [SerializeField] private AudioClip _deathSound;            // ★ [New] 사망 사운드
    [SerializeField] private float _deathDuration = 4.5f;
    [SerializeField] private float _floatHeight = 50f;
    [Range(0f, 1f)][SerializeField] private float _rotationStartTime = 0.5f;
    [SerializeField] private float _totalRotationAngle = 1080f;

    // 컴포넌트 캐싱
    private Renderer[] _renderers;
    private Rigidbody _rb;
    private Collider _col;
    private AudioSource _audioSource;

    // 피격 피드백용 변수
    private Coroutine _damageFlashCoroutine;
    private Dictionary<Material, Color> _originalColorCache = new Dictionary<Material, Color>();
    private bool _isFlashing = false;

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
    [SerializeField] private float _maxUiWidth = 600f;

    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _staminaText;
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _killText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _expText;

    // ==========================================
    // 6. 프로퍼티 로직
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
        _wasOverweight = IsOverweight;

        // 컴포넌트 캐싱
        _renderers = GetComponentsInChildren<Renderer>();
        _cachedController = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        // AudioSource 안전하게 가져오기
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

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
        if (_expBarCircular != null) _expBarCircular.fillAmount = (float)_currentExp / _maxExp;

        if (_hpBarRect != null)
        {
            float width = Mathf.Min(MaxHp * _barWidthMultiplier, _maxUiWidth);
            _hpBarRect.sizeDelta = new Vector2(width, _hpBarRect.sizeDelta.y);
        }

        if (_staminaBarRect != null)
        {
            float width = Mathf.Min(MaxStamina * _barWidthMultiplier, _maxUiWidth);
            _staminaBarRect.sizeDelta = new Vector2(width, _staminaBarRect.sizeDelta.y);
        }

        if (_hpText != null) _hpText.text = $"{_currentHp:F0} / {MaxHp:F0}";
        if (_staminaText != null) _staminaText.text = $"{_currentStamina:F0} / {MaxStamina:F0}";
        if (_coinText != null) _coinText.text = $"{_coin}";
        if (_killText != null) _killText.text = $"{_killCount}";
        if (_levelText != null) _levelText.text = $"Lv.{_level}";
        if (_expText != null) _expText.text = $"{_currentExp} / {_maxExp}";
    }

    // ==========================================
    // 9. 전투 및 회복
    // ==========================================
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection, float knockbackForce)
    {
        if (_isDead) return;
        int finalDamage = Mathf.Max(1, damage - _def);
        Hp -= finalDamage;

        PlayDamageFeedback();
    }

    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection) => TakeDamage(damage, hitPoint, attackDirection, 0f);
    public void TakeDamage(int damage) => TakeDamage(damage, transform.position, Vector3.zero, 0f);

    private void PlayDamageFeedback()
    {
        if (_hurtSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_hurtSound);
        }

        if (_damageFlashCoroutine != null)
        {
            StopCoroutine(_damageFlashCoroutine);
        }
        _damageFlashCoroutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (!_isFlashing)
        {
            _originalColorCache.Clear();
            foreach (var renderer in _renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        _originalColorCache[mat] = mat.color;
                    }
                }
            }
            _isFlashing = true;
        }

        foreach (var renderer in _renderers)
        {
            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    mat.color = _damageFlashColor;
                }
            }
        }

        yield return new WaitForSeconds(_flashDuration);

        foreach (var kvp in _originalColorCache)
        {
            if (kvp.Key != null)
            {
                kvp.Key.color = kvp.Value;
            }
        }

        _isFlashing = false;
        _damageFlashCoroutine = null;
    }

    public void Heal(float amount) { if (!_isDead) Hp += amount; }
    public void RestoreStamina(float amount) { if (!_isDead) Stamina += amount; }
    public void ConsumeStamina(float amount) { if (!_isDead) Stamina -= amount; }

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
        if (_isDead) return;
        _isDead = true;
        Debug.Log("Player Died.");

        if (_cachedController != null) _cachedController.enabled = false;
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        if (_col != null) _col.enabled = false;

        StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        // ★ [New] 사망 사운드 재생
        if (_deathSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_deathSound);
        }

        // ★ [New] 사망 임팩트 이펙트 (폭발 등)
        if (_deathImpactVfxPrefab != null)
        {
            Instantiate(_deathImpactVfxPrefab, transform.position, Quaternion.identity);
        }

        // 기존: 영혼 승천 이펙트
        if (_deathVfxPrefab != null)
        {
            Instantiate(_deathVfxPrefab, transform.position, Quaternion.identity);
        }

        float timer = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * _floatHeight;
        Quaternion startRotation = transform.rotation;

        while (timer < _deathDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / _deathDuration);

            transform.position = Vector3.Lerp(startPos, targetPos, progress);

            if (_renderers != null)
            {
                foreach (Renderer r in _renderers)
                {
                    foreach (Material m in r.materials)
                    {
                        if (m.HasProperty("_Color"))
                        {
                            Color c = m.color;
                            c.a = Mathf.Lerp(1f, 0f, progress);
                            m.color = c;
                        }
                    }
                }
            }

            if (progress >= _rotationStartTime)
            {
                float rotationProgress = (progress - _rotationStartTime) / (1.0f - _rotationStartTime);
                float easedProgress = Mathf.Pow(rotationProgress, 3);
                float targetYRotation = Mathf.Lerp(0f, _totalRotationAngle, easedProgress);
                transform.rotation = startRotation * Quaternion.Euler(0f, targetYRotation, 0f);
            }

            yield return null;
        }

        Destroy(gameObject);
        OnPlayerDeathSequenceCompleted?.Invoke();
    }

    // ==========================================
    // 10. 재화 및 성장
    // ==========================================
    public void GainCoin(int amount) { _coin += amount; UpdateUI(); }
    public void AddKill() { _killCount++; UpdateUI(); }
    public bool UseCoin(int amount)
    {
        if (_coin >= amount) { _coin -= amount; UpdateUI(); return true; }
        return false;
    }
    public void GainExp(int amount)
    {
        _currentExp += amount;
        while (_currentExp >= _maxExp) { LevelUp(); }
        UpdateUI();
    }
    private void LevelUp()
    {
        _currentExp -= _maxExp;
        _level++;
        _maxExp += 50;
        _statPoint += 5;
        Hp = MaxHp;
        Stamina = MaxStamina;
        PlayLevelUpEffect();
    }
    private void PlayLevelUpEffect()
    {
        Vector3 spawnPos = transform.position + _effectOffset + Vector3.up * 1.0f;
        if (_levelUpSound != null) AudioSource.PlayClipAtPoint(_levelUpSound, spawnPos, 1.0f);
        if (_levelUpVfxPrefab != null)
        {
            GameObject vfx = Instantiate(_levelUpVfxPrefab, spawnPos, Quaternion.identity);
            vfx.transform.SetParent(transform);
            Destroy(vfx, 2.0f);
        }
    }

    // ==========================================
    // 11. 업그레이드 및 아이템 획득
    // ==========================================
    public bool TryUpgradeAtk()
    {
        if (_statPoint > 0) { _atk += 1; _statPoint--; return true; }
        return false;
    }
    public bool TryUpgradeHp()
    {
        if (_statPoint > 0) { MaxHp += 10f; Hp = MaxHp; UpdateUI(); _statPoint--; return true; }
        return false;
    }
    public bool TryUpgradeStamina()
    {
        if (_statPoint > 0) { MaxStamina += 10f; Stamina = MaxStamina; UpdateUI(); _statPoint--; return true; }
        return false;
    }
    public bool TryUpgradeSpeed()
    {
        if (_statPoint > 0)
        {
            if (_cachedController != null) { _cachedController.UpgradeSpeed(0.5f); _statPoint--; return true; }
        }
        return false;
    }
    public void UpgradeAtk(int amount) => _atk += amount;
    public void UpgradeHp(float amount) { MaxHp += amount; Hp = MaxHp; UpdateUI(); }
    public void UpgradeStamina(float amount) { MaxStamina += amount; Stamina = MaxStamina; UpdateUI(); }
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
        if (IsOverweight) return 0.6f;
        return 1.0f;
    }
    public void UpdateWeight(float newWeight)
    {
        _currentWeight = newWeight;
        bool isCurrentlyOverweight = IsOverweight;
        if (isCurrentlyOverweight != _wasOverweight)
        {
            _wasOverweight = isCurrentlyOverweight;
        }
    }

    public void ExpandMaxWeight(float amount)
    {
        _maxWeight += amount;
        if (InventoryUI.Instance != null) InventoryUI.Instance.UpdateWeightText();
    }
}