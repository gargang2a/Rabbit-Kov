using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Player : MonoBehaviour, IDamageable
{
    // ★ [추가 1] "플레이어가 사망했다"는 사실을 외부에 알릴 정적 이벤트
    public static event Action OnPlayerDeathSequenceCompleted;
    // ==========================================
    // 1. 레벨 및 경험치
    // ==========================================
    [Header("Level & Exp / 레벨 & 경험치")]
    [Tooltip("플레이어 현재 레벨")]
    [SerializeField] private int _level = 1;
    [Tooltip("현재 경험치")]
    [SerializeField] private int _currentExp = 0;
    [Tooltip("다음 레벨까지 필요한 경험치")]
    [SerializeField] private int _maxExp = 100;

    [Header("Growth System / 성장 시스템")]
    [Tooltip("획득 가능한 스탯 포인트")]
    [SerializeField] private int _statPoint = 0;
    [Tooltip("탄퍼짐 감소량 (예: 수직 그립 장착 시)")]
    [SerializeField] private float _spreadReduction = 0f;

    // ★ UI 호환성을 위해 프로퍼티 복구
    public int Level => _level;
    public int CurrentExp => _currentExp;
    public int Exp => _currentExp;
    public int MaxExp => _maxExp;
    public int StatPoint => _statPoint;
    public float SpreadReduction => _spreadReduction;

    // ==========================================
    // 2. 기본 스탯
    // ==========================================
    [Header("Player Stats / 플레이어 스탯")]
    [Tooltip("현재 체력")]
    [SerializeField] private float _currentHp;
    [Tooltip("현재 스태미나")]
    [SerializeField] private float _currentStamina;
    [Tooltip("스태미나 회복 속도 (초당)")]
    [SerializeField] private float _staminaRegenSpeed = 20f;

    public float MaxHp { get; private set; } = 100f;
    public float MaxStamina { get; private set; } = 100f;

    [Header("Battle Stats / 전투 스탯")]
    [Tooltip("기본 공격력")]
    [SerializeField] private int _atk = 10;
    [Tooltip("기본 방어력")]
    [SerializeField] private int _def;
    [Tooltip("기본 쉴드 값")]
    [SerializeField] private int _shield;

    // ★ [Compatibility] UI 스크립트들이 찾는 변수명 연결
    public int Atk => _atk;        // StatUpgradeUI용
    public int BaseAttack => _atk; // PlayerStatusUI용
    public int Def => _def;
    public int Shield => _shield;

    // ★ [New] 이동속도 UI 표시용 프로퍼티
    // PlayerController에게 현재 속도(무게 페널티 포함)를 물어봐서 반환합니다.
    public float MoveSpeed
    {
        get
        {
            PlayerController pc = GetComponent<PlayerController>();
            return pc != null ? pc.CurrentMoveSpeed : 0f;
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

    [Header("Death Settings")]
    [SerializeField] private GameObject _deathVfxPrefab;
    [SerializeField] private float _deathDuration = 2.5f;
    [SerializeField] private float _floatHeight = 2.0f;
    [Range(0f, 1f)][SerializeField] private float _rotationStartTime = 0.3f;
    [SerializeField] private float _totalRotationAngle = 1080f;

    private Renderer[] _renderers;

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
    // 6. 프로퍼티 (HP/Stamina)
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
        _renderers = GetComponentsInChildren<Renderer>();
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
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection, float knockbackForce)
    {
        if (_isDead) return;
        int finalDamage = Mathf.Max(1, damage - _def);
        Hp -= finalDamage;
    }

    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection) => TakeDamage(damage, hitPoint, attackDirection, 0f);
    public void TakeDamage(int damage) => TakeDamage(damage, transform.position, Vector3.zero, 0f);
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
        StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (_deathVfxPrefab != null) Instantiate(_deathVfxPrefab, transform.position, Quaternion.identity);

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
        // ★ [추가 2] 코루틴의 마지막 순간에 이벤트를 방송합니다.
        OnPlayerDeathSequenceCompleted?.Invoke();
    }

    // ==========================================
    // 10. 재화 및 성장
    // ==========================================
    public void GainCoin(int amount) { _coin += amount; UpdateUI(); }
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
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null) { pc.UpgradeSpeed(0.5f); _statPoint--; return true; }
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

    // UI 강제 갱신 요청 포함
    public void ExpandMaxWeight(float amount)
    {
        _maxWeight += amount;
        if (InventoryUI.Instance != null) InventoryUI.Instance.UpdateWeightText();
    }
}