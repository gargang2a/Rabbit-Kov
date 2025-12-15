using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // ★ TextMeshPro 사용 필수

public class Player : MonoBehaviour
{
    // ==========================================
    // 1. 레벨 및 경험치 (New!)
    // ==========================================
    [Header("Level & Exp")]
    [SerializeField] private int level = 1;
    [SerializeField] private int exp = 0;
    [SerializeField] private int maxExp = 100; // 다음 레벨까지 필요한 경험치

    // 외부에서 읽기 전용
    public int Level => level;
    public int Exp => exp;
    public int MaxExp => maxExp;

    // ==========================================
    // 2. 기본 스탯 설정
    // ==========================================
    [Header("Player Info (기본 정보)")]
    [SerializeField] private float hp;
    [SerializeField] private float stamina;

    public float MaxHp { get; private set; } = 100f;
    public float MaxStamina { get; private set; } = 100f;
    [SerializeField] private float staminaRegenSpeed = 20f;

    [Header("Battle Stats (전투 스탯)")]
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
    [Header("Condition (상태 이상)")]
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
    [Header("UI References (연결 필요)")]
    public Image hpBarImage;
    public Image staminaBarImage;

    public TMP_Text hpText;
    public TMP_Text staminaText;
    public TMP_Text coinText;

    // (선택 사항) 레벨이나 경험치를 표시할 텍스트가 있다면 연결하세요
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

            if (hp <= 0 && !isDead)
            {
                hp = 0;
                Die();
            }
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
        // 초기화
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
    // 7. UI 업데이트 로직
    // ==========================================
    private void UpdateUI()
    {
        // 1. 바(Bar) 갱신
        if (hpBarImage != null) hpBarImage.fillAmount = hp / MaxHp;
        if (staminaBarImage != null) staminaBarImage.fillAmount = stamina / MaxStamina;

        // 2. 텍스트 갱신
        if (hpText != null) hpText.text = $"{hp:F0} / {MaxHp:F0}";
        if (staminaText != null) staminaText.text = $"{stamina:F0} / {MaxStamina:F0}";
        if (coinText != null) coinText.text = $"Coin: {coin}";

        // 3. 레벨/경험치 텍스트 갱신 (연결되어 있다면)
        if (levelText != null) levelText.text = $"Lv.{level}";
        if (expText != null) expText.text = $"{exp} / {maxExp}";
    }

    // ==========================================
    // 8. 전투 및 행동 함수
    // ==========================================
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        int finalDamage = Mathf.Max(1, damage - Def);
        Hp -= finalDamage;
        Debug.Log($"플레이어 피격! 남은 체력: {Hp}");
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
        isDead = true;
        Debug.Log("플레이어 사망");
        // GetComponent<Animator>().SetTrigger("Die");
    }

    // ==========================================
    // 9. 재화 및 업그레이드 함수
    // ==========================================

    public void GainCoin(int amount)
    {
        coin += amount;
        UpdateUI();
    }

    public bool UseCoin(int amount)
    {
        if (coin >= amount)
        {
            coin -= amount;
            UpdateUI();
            return true;
        }
        else
        {
            Debug.Log("코인이 부족합니다!");
            return false;
        }
    }

    public void UpgradeAtk(int amount)
    {
        atk += amount;
        Debug.Log($"공격력 업그레이드! 현재: {atk}");
    }

    public void UpgradeStamina(float amount)
    {
        MaxStamina += amount;
        Stamina = MaxStamina;
        UpdateUI();
        Debug.Log($"스태미너 업그레이드! 현재: {MaxStamina}");
    }

    public void UpgradeHp(float amount)
    {
        MaxHp += amount;
        Hp = MaxHp;
        UpdateUI();
        Debug.Log($"체력 업그레이드! 현재: {MaxHp}");
    }

    // ==========================================
    // ★ 10. 경험치 획득 및 레벨업 (New!)
    // ==========================================
    public void GainExp(int amount)
    {
        exp += amount;
        Debug.Log($"경험치 획득! (+{amount}) 현재 EXP: {exp} / {maxExp}");

        // 경험치가 최대치를 넘으면 레벨업 (반복문: 한번에 경험치를 많이 먹었을 경우 대비)
        while (exp >= maxExp)
        {
            LevelUp();
        }

        UpdateUI();
    }

    private void LevelUp()
    {
        exp -= maxExp;      // 남은 경험치 이월
        level++;            // 레벨 증가
        maxExp += 50;       // 다음 레벨 필요 경험치 증가 (난이도 조절)

        // 레벨업 보너스 (예: 체력/스태미너 완전 회복)
        Hp = MaxHp;
        Stamina = MaxStamina;

        Debug.Log($" 레벨 업! 현재 레벨: {level}");

        // 여기에 레벨업 효과음이나 파티클 재생 로직 추가 가능
    }
}