using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // ★ TextMeshPro 필수

public class Player : MonoBehaviour
{
    // ==========================================
    // 1. 레벨 및 경험치
    // ==========================================
    [Header("Level & Exp")]
    [SerializeField] private int level = 1;
    [SerializeField] private int exp = 0;
    [SerializeField] private int maxExp = 100;

    public int Level => level;
    public int Exp => exp;
    public int MaxExp => maxExp;

    // ==========================================
    // 2. 기본 스탯 설정
    // ==========================================
    [Header("Player Info")]
    [SerializeField] private float hp;
    [SerializeField] private float stamina;

    public float MaxHp { get; private set; } = 100f;
    public float MaxStamina { get; private set; } = 100f;
    [SerializeField] private float staminaRegenSpeed = 20f;

    [Header("Battle Stats")]
    [SerializeField] private int atk;
    [SerializeField] private int def;
    [SerializeField] private int shield;

    public int Atk => atk;
    public int Def => def;
    public int Shield => shield;

    // ==========================================
    // 3. 상태 및 인벤토리
    // ==========================================
    [Space]
    [Header("Condition")]
    [SerializeField] private bool _isDead = false;
    public bool isDead
    {
        get => _isDead;
        private set => _isDead = value;
    }

    [Space]
    [Header("Inventory (재화)")]
    [SerializeField] private int coin = 0;
    public int Coin => coin;

    [SerializeField] private string slotOne;
    [SerializeField] private string slotTwo;
    [SerializeField] private string slotThree;
    [SerializeField] private string slotFour;

    // ==========================================
    // 4. UI 연결
    // ==========================================
    [Space]
    [Header("UI References")]
    public Image hpBarImage;
    public Image staminaBarImage;

    public TMP_Text hpText;
    public TMP_Text staminaText;
    public TMP_Text coinText;

    public TMP_Text levelText;
    public TMP_Text expText;

    // ==========================================
    // 5. 프로퍼티 (값 변경 시 로직)
    // ==========================================
    public float Hp
    {
        get => hp;
        set
        {
            hp = Mathf.Clamp(value, 0, MaxHp);
            UpdateUI();
            if (hp <= 0 && !isDead) { hp = 0; Die(); }
        }
    }

    public float Stamina
    {
        get => stamina;
        set
        {
            stamina = Mathf.Clamp(value, 0, MaxStamina);
            UpdateUI();
            if (stamina <= 0) stamina = 0;
        }
    }

    // ==========================================
    // 6. 유니티 라이프사이클
    // ==========================================
    private void Awake()
    {
        Hp = MaxHp;
        Stamina = MaxStamina;
        isDead = false;
        UpdateUI();
    }

    private void Update()
    {
        if (isDead) return;

        // 스태미너 자동 회복
        if (Stamina < MaxStamina)
        {
            Stamina += staminaRegenSpeed * Time.deltaTime;
        }
    }

    // ==========================================
    // 7. UI 업데이트
    // ==========================================
    private void UpdateUI()
    {
        if (hpBarImage != null) hpBarImage.fillAmount = hp / MaxHp;
        if (staminaBarImage != null) staminaBarImage.fillAmount = stamina / MaxStamina;

        if (hpText != null) hpText.text = $"{hp:F0} / {MaxHp:F0}";
        if (staminaText != null) staminaText.text = $"{stamina:F0} / {MaxStamina:F0}";
        if (coinText != null) coinText.text = $"{coin}";

        if (levelText != null) levelText.text = $"Lv.{level}";
        if (expText != null) expText.text = $"{exp} / {maxExp}";
    }

    // ==========================================
    // 8. 기능 함수들
    // ==========================================
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        int finalDamage = Mathf.Max(1, damage - Def);
        Hp -= finalDamage;
    }

    public bool UseStamina(int amount)
    {
        if (Stamina >= amount) { Stamina -= amount; return true; }
        return false;
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("플레이어 사망");
    }

    // ==========================================
    // 9. 재화 및 업그레이드
    // ==========================================
    public void GainCoin(int amount)
    {
        coin += amount;
        UpdateUI();
    }

    public bool UseCoin(int amount)
    {
        if (coin >= amount) { coin -= amount; UpdateUI(); return true; }
        return false;
    }

    public void UpgradeAtk(int amount) { atk += amount; }

    public void UpgradeStamina(float amount)
    {
        MaxStamina += amount;
        Stamina = MaxStamina;
        UpdateUI();
    }

    public void UpgradeHp(float amount)
    {
        MaxHp += amount;
        Hp = MaxHp;
        UpdateUI();
    }

    // ==========================================
    // 10. 경험치 획득 및 레벨업
    // ==========================================
    public void GainExp(int amount)
    {
        exp += amount;
        while (exp >= maxExp) LevelUp();
        UpdateUI();
    }

    private void LevelUp()
    {
        exp -= maxExp;
        level++;
        maxExp += 50;
        Hp = MaxHp;
        Stamina = MaxStamina;
        Debug.Log($"레벨 업! Lv.{level}");
    }
 }    
