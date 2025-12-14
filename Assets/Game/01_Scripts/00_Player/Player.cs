using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // ★ [중요] TextMeshPro를 쓰기 위해 추가됨

public class Player : MonoBehaviour
{
    // ★ 이동 관련 변수 및 Rigidbody 삭제됨 (PlayerController에서 담당)

    [Header("Player Info 정보")]
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

    [Space]
    [Header("Condition 상태")]
    [SerializeField] private bool isBleed;
    [SerializeField] private bool isSlow;
    public bool isDead { get; private set; } = false;

    [Space]
    [Header("Inventory 인벤토리")]
    [SerializeField] private int coin;
    [SerializeField] private string slotOne;
    [SerializeField] private string slotTwo;
    [SerializeField] private string slotThree;
    [SerializeField] private string slotFour;

    [Space]
    [Header("UI References (연결 필요)")]
    // 이미지를 사용하므로 Image 타입 유지
    public Image hpBarImage;
    public Image staminaBarImage;

    // ★ [수정됨] Text -> TMP_Text 로 변경 (이제 드래그가 될 겁니다!)
    public TMP_Text hpText;
    public TMP_Text staminaText;

    // HP 프로퍼티
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

    // Stamina 프로퍼티
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

    private void Awake()
    {
        Hp = MaxHp;
        Stamina = MaxStamina;
        isDead = false;
    }

    private void Update()
    {
        if (isDead) return;

        if (Stamina < MaxStamina)
        {
            Stamina += staminaRegenSpeed * Time.deltaTime;
        }
    }

    // UI 갱신 함수
    private void UpdateUI()
    {
        // 이미지(Filled) 갱신
        if (hpBarImage != null) hpBarImage.fillAmount = hp / MaxHp;
        if (staminaBarImage != null) staminaBarImage.fillAmount = stamina / MaxStamina;

        // 텍스트(TMP) 갱신
        if (hpText != null) hpText.text = $"{hp:F0} / {MaxHp}";
        if (staminaText != null) staminaText.text = $"{stamina:F0} / {MaxStamina}";
    }

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
    }
}