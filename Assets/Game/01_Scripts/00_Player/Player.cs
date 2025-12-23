using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Player : MonoBehaviour, IDamageable
{
    // ★ [추가 1] "플레이어가 사망했다"는 사실을 외부에 알릴 정적(static) 이벤트
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

    public int Level => _level;
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
    [SerializeField] private int _atk;
    [Tooltip("기본 방어력")]
    [SerializeField] private int _def;
    [Tooltip("기본 쉴드 값")]
    [SerializeField] private int _shield;

    public int Atk => _atk;
    public int Def => _def;
    public int Shield => _shield;

    // ==========================================
    // 3. 상태 및 인벤토리
    // ==========================================
    [Space]
    [Header("Condition / 상태")]
    [Tooltip("플레이어 사망 여부")]
    [SerializeField] private bool _isDead = false;
    public bool IsDead => _isDead;

    [Space]
    [Header("Inventory & Weight / 인벤토리 & 무게")]
    [Tooltip("보유한 골드(재화)")]
    [SerializeField] private int _coin = 0;
    public int Coin => _coin;

    [Tooltip("최대 소지 무게")]
    [SerializeField] private float _maxWeight = 50f;
    [Tooltip("현재 소지 무게")]
    [SerializeField] private float _currentWeight = 0f;

    [Tooltip("몇 퍼센트부터 무게 초과인지 설정 (0.0 ~ 1.0)")]
    [Range(0f, 1f)][SerializeField] private float _overweightThreshold = 0.8f;

    private bool _wasOverweight = false;

    public float MaxWeight => _maxWeight;
    public float CurrentWeight => _currentWeight;
    public bool IsOverweight => _currentWeight >= _maxWeight * _overweightThreshold;

    // ==========================================
    // 4. 이펙트 및 오디오 (사망 연출 포함)
    // ==========================================
    [Space]
    [Header("Effects & Audio / 이펙트 & 오디오")]
    [Tooltip("레벨업 시 생성할 VFX 프리팹")]
    [SerializeField] private GameObject _levelUpVfxPrefab;
    [Tooltip("레벨업 시 재생할 오디오 클립")]
    [SerializeField] private AudioClip _levelUpSound;
    [Tooltip("이펙트 생성 위치 오프셋")]
    [SerializeField] private Vector3 _effectOffset = Vector3.zero;

    [Header("Death Settings / 사망 설정")]
    [Tooltip("사망 시 생성될 VFX 프리팹")]
    [SerializeField] private GameObject _deathVfxPrefab;

    [Tooltip("사망 연출 지속 시간 (초)")]
    [SerializeField] private float _deathDuration = 2.5f;

    [Tooltip("사망 시 위로 떠오르는 높이")]
    [SerializeField] private float _floatHeight = 2.0f;

    [Space]
    [Tooltip("회전이 시작될 시점 (0.0 ~ 1.0, 0.3이면 30% 지점부터 회전)")]
    [Range(0f, 1f)]
    [SerializeField] private float _rotationStartTime = 0.3f;

    [Tooltip("총 회전 각도 (360 = 1바퀴, 1080 = 3바퀴)")]
    [SerializeField] private float _totalRotationAngle = 1080f;

    private Renderer[] _renderers;

    // ==========================================
    // 5. UI 참조
    // ==========================================
    [Space]
    [Header("UI References / UI 참조")]
    [Tooltip("HP 바 이미지 (fillAmount 사용)")]
    [SerializeField] private Image _hpBarImage;
    [Tooltip("스태미나 바 이미지 (fillAmount 사용)")]
    [SerializeField] private Image _staminaBarImage;
    [Tooltip("원형 경험치 바 이미지 (fillAmount 사용)")]
    [SerializeField] private Image _expBarCircular;
    [Tooltip("HP 바 RectTransform (크기 조절용)")]
    [SerializeField] private RectTransform _hpBarRect;
    [Tooltip("스태미나 바 RectTransform (크기 조절용)")]
    [SerializeField] private RectTransform _staminaBarRect;
    [Tooltip("바 너비 계산 시 곱할 값 (Max * multiplier = 실제 너비)")]
    [SerializeField] private float _barWidthMultiplier = 2.0f;
    [Tooltip("HP 표시용 텍스트 (TextMeshPro)")]
    [SerializeField] private TMP_Text _hpText;
    [Tooltip("스태미나 표시용 텍스트 (TextMeshPro)")]
    [SerializeField] private TMP_Text _staminaText;
    [Tooltip("골드 표시용 텍스트 (TextMeshPro)")]
    [SerializeField] private TMP_Text _coinText;
    [Tooltip("레벨 표시용 텍스트 (TextMeshPro)")]
    [SerializeField] private TMP_Text _levelText;
    [Tooltip("경험치 표시용 텍스트 (TextMeshPro)")]
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
        Debug.Log("Player Died. Starting Death Sequence.");
        StartCoroutine(DeathSequenceRoutine());
    }

    // ★★★ [복구됨] 사망 연출 코루틴 (부유 -> 투명화 -> 가속 회전 -> 삭제)
    // 크기 축소(Scale) 로직 제거됨
    private IEnumerator DeathSequenceRoutine()
    {
        // 1. 물리 충돌 및 조작 비활성화
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 2. 사망 VFX 재생
        if (_deathVfxPrefab != null)
        {
            Instantiate(_deathVfxPrefab, transform.position, Quaternion.identity);
        }

        // 3. 부유, 투명화, 회전 루프
        float timer = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * _floatHeight;
        Quaternion startRotation = transform.rotation;

        while (timer < _deathDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / _deathDuration);

            // A. 위로 천천히 이동 (Lerp)
            transform.position = Vector3.Lerp(startPos, targetPos, progress);

            // B. 투명화 처리 (Alpha값 조정)
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

            // C. Y축 가속 회전 처리 ("휘릭" 효과)
            if (progress >= _rotationStartTime)
            {
                float rotationProgress = (progress - _rotationStartTime) / (1.0f - _rotationStartTime);

                // 가속도 적용 (Cubic Ease-In)
                float easedProgress = Mathf.Pow(rotationProgress, 3);

                // 0도 ~ 1080도(3바퀴) 회전
                float targetYRotation = Mathf.Lerp(0f, _totalRotationAngle, easedProgress);

                transform.rotation = startRotation * Quaternion.Euler(0f, targetYRotation, 0f);
            }

            yield return null;
        }

        // 4. 완전히 사라짐 (오브젝트 삭제)
        Debug.Log("Player Object Destroyed.");
        Destroy(gameObject);
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
        Debug.Log($"Level Up! Current Level: {_level}, Point: {_statPoint}");
        PlayLevelUpEffect();
    }
    private void PlayLevelUpEffect()
    {
        Debug.Log($"[Player] 레벨업 이펙트 실행! (Level: {_level})");
        Vector3 spawnPos = transform.position + _effectOffset + Vector3.up * 1.0f;
        if (_levelUpSound != null) AudioSource.PlayClipAtPoint(_levelUpSound, spawnPos, 1.0f);
        if (_levelUpVfxPrefab != null)
        {
            GameObject vfx = Instantiate(_levelUpVfxPrefab, spawnPos, Quaternion.identity);
            vfx.transform.SetParent(transform);
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null) { ps.Play(); Destroy(vfx, ps.main.duration + 0.5f); }
            else { Destroy(vfx, 2.0f); }
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
            ApplyMovementDebuff(isCurrentlyOverweight);
            _wasOverweight = isCurrentlyOverweight;
        }
    }
    public void ExpandMaxWeight(float amount)
    {
        _maxWeight += amount;
        UpdateWeight(_currentWeight);
    }
    private void ApplyMovementDebuff(bool isHeavy)
    {
        Debug.Log(isHeavy ? "무게 초과! 이동 속도가 40% 감소했습니다." : "무게 정상화. 이동 속도 디버프 해제.");
    }
}