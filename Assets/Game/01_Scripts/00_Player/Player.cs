using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // â˜… TextMeshPro í•„ìˆ˜

public class Player : MonoBehaviour, IDamageable
{
    // ==========================================
    // 1. ë ˆë²¨ ë° ê²½í—˜ì¹˜
    // ==========================================
    [Header("Level & Exp")]
    [SerializeField] private int level = 1;
    [SerializeField] private int exp = 0;
    [SerializeField] private int maxExp = 100;

    public int Level => level;
    public int Exp => exp;
    public int MaxExp => maxExp;

    // ==========================================
    // 2. ê¸°ë³¸ ìŠ¤íƒ¯ ì„¤ì •
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
    // 3. ìƒíƒœ ë° ì¸ë²¤í† ë¦¬
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
    [Header("Inventory (ìž¬í™”)")]
    [SerializeField] private int coin = 0;
    public int Coin => coin;

    [SerializeField] private string slotOne;
    [SerializeField] private string slotTwo;
    [SerializeField] private string slotThree;
    [SerializeField] private string slotFour;

    // ==========================================
    // 4. UI ì—°ê²°
    // ==========================================
    [Space]
    [Header("UI References")]
    public Image hpBarImage;
    public Image staminaBarImage;

    // ¡Ú [Ãß°¡µÊ] ¹ÙÀÇ ½ÇÁ¦ ±æÀÌ(Size)¸¦ Á¶ÀýÇÏ±â À§ÇÑ RectTransform
    public RectTransform hpBarRect;
    public RectTransform staminaBarRect;

    // ¡Ú [Ãß°¡µÊ] Ã¼·Â/½ºÅÂ¹Ì³Ê 1´ç ´Ã¾î³¯ ±æÀÌ (ÇÈ¼¿ ´ÜÀ§, ±âº»°ª 2.0)
    [Header("UI Settings")]
    public float barWidthMultiplier = 2.0f;

    // ¿øÇü °æÇèÄ¡ ¹Ù
    public Image expBarCircular;

    public TMP_Text hpText;
    public TMP_Text staminaText;
    public TMP_Text coinText;

    public TMP_Text levelText;
    public TMP_Text expText;

    // ==========================================
    // 5. í”„ë¡œí¼í‹° (ê°’ ë³€ê²½ ì‹œ ë¡œì§)
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
    // 6. ìœ ë‹ˆí‹° ë¼ì´í”„ì‚¬ì´í´
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

        // ìŠ¤íƒœë¯¸ë„ˆ ìžë™ íšŒë³µ
        if (Stamina < MaxStamina)
        {
            Stamina += staminaRegenSpeed * Time.deltaTime;
        }
    }

    // ==========================================
    // 7. UI ì—…ë°ì´íŠ¸
    // ==========================================
    private void UpdateUI()
    {
        // 1. ¼±Çü ¹Ù(Bar) Ã¤¿ì±â °»½Å (ºñÀ²)
        if (hpBarImage != null) hpBarImage.fillAmount = hp / MaxHp;
        if (staminaBarImage != null) staminaBarImage.fillAmount = stamina / MaxStamina;

        // ¡Ú [Ãß°¡µÊ] ÃÖ´ëÄ¡¿¡ µû¶ó ¹ÙÀÇ °¡·Î ±æÀÌ(Width) ´Ã¸®±â
        if (hpBarRect != null)
        {
            // ³Êºñ = ÃÖ´ëÃ¼·Â * ¹èÀ², ³ôÀÌ´Â ±âÁ¸ À¯Áö
            hpBarRect.sizeDelta = new Vector2(MaxHp * barWidthMultiplier, hpBarRect.sizeDelta.y);
        }

        if (staminaBarRect != null)
        {
            staminaBarRect.sizeDelta = new Vector2(MaxStamina * barWidthMultiplier, staminaBarRect.sizeDelta.y);
        }

        // 2. ¿øÇü °æÇèÄ¡ ¹Ù °»½Å (0.0 ~ 1.0)
        if (expBarCircular != null)
        {
            expBarCircular.fillAmount = (float)exp / (float)maxExp;
        }

        // 3. ÅØ½ºÆ® °»½Å
        if (hpText != null) hpText.text = $"{hp:F0} / {MaxHp:F0}";
        if (staminaText != null) staminaText.text = $"{stamina:F0} / {MaxStamina:F0}";
        if (coinText != null) coinText.text = $"{coin}";

        if (levelText != null) levelText.text = $"Lv.{level}";
        if (expText != null) expText.text = $"{exp} / {maxExp}";
    }

    // ==========================================
    // 8. ê¸°ëŠ¥ í•¨ìˆ˜ë“¤ (IDamageable êµ¬í˜„)
    // ==========================================
    
    // IDamageable - ê°„ë‹¨ ë²„ì „
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        int finalDamage = Mathf.Max(1, damage - Def);
        Hp -= finalDamage;
    }
    
    // IDamageable - ìƒì„¸ ë²„ì „ (ë„‰ë°±/í”¼ê²© ì´íŽ™íŠ¸ìš©)
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection)
    {
        if (isDead) return;
        int finalDamage = Mathf.Max(1, damage - Def);
        Hp -= finalDamage;
        
        // TODO: í”¼ê²© ì´íŽ™íŠ¸, ë„‰ë°± ì²˜ë¦¬
        Debug.Log($"Player hit! Damage: {finalDamage}, Direction: {attackDirection}");
    }

    public bool UseStamina(int amount)
    {
        if (Stamina >= amount) { Stamina -= amount; return true; }
        return false;
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("í”Œë ˆì´ì–´ ì‚¬ë§");
    }

    // ==========================================
    // 9. ìž¬í™” ë° ì—…ê·¸ë ˆì´ë“œ
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
        Stamina = MaxStamina; // ¾÷±×·¹ÀÌµå ½Ã ÇöÀç ½ºÅÂ¹Ì³Êµµ Ã¤¿öÁÜ
        UpdateUI();
    }

    public void UpgradeHp(float amount)
    {
        MaxHp += amount;
        Hp = MaxHp; // ¾÷±×·¹ÀÌµå ½Ã ÇöÀç Ã¼·Âµµ Ã¤¿öÁÜ
        UpdateUI();
    }

    // ==========================================
    // 10. ê²½í—˜ì¹˜ íšë“ ë° ë ˆë²¨ì—…
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
        Debug.Log($"ë ˆë²¨ ì—…! Lv.{level}");
    }
}