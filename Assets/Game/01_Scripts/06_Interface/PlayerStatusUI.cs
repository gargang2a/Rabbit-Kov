using UnityEngine;
using TMPro;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("UI References (Text)")]
    [SerializeField] private TMP_Text _statAttackText;
    [SerializeField] private TMP_Text _weaponAttackText;
    [SerializeField] private TMP_Text _fireRateText;
    [SerializeField] private TMP_Text _recoilText;
    [SerializeField] private TMP_Text _weightText;
    [SerializeField] private TMP_Text _expText;

    // ★ [New] 속도 표시용 텍스트
    [SerializeField] private TMP_Text _speedText;

    [Header("System References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerWeaponController _weaponController;

    private void Start()
    {
        if (_player == null) _player = FindObjectOfType<Player>();
        if (_weaponController == null) _weaponController = FindObjectOfType<PlayerWeaponController>();
    }

    private void Update()
    {
        if (_player == null) return;

        UpdatePlayerStats();
        UpdateWeaponStats();
    }

    private void UpdatePlayerStats()
    {
        if (_statAttackText != null)
            _statAttackText.text = _player.BaseAttack.ToString();

        if (_expText != null)
            _expText.text = _player.CurrentExp.ToString();

        if (_weightText != null)
            _weightText.text = $"{_player.CurrentWeight:F1} / {_player.MaxWeight:F0}";

        // ★ [New] 이동속도 표시 (소수점 1자리까지)
        // 무게가 늘어나면 Player.MoveSpeed 값이 자동으로 줄어들어 표시됩니다.
        SetText(_speedText, _player.MoveSpeed.ToString("F1"));
    }

    private void UpdateWeaponStats()
    {
        if (_weaponController == null || _weaponController.CurrentWeapon == null)
        {
            SetText(_weaponAttackText, "0");
            SetText(_fireRateText, "-");
            SetText(_recoilText, "-");
            return;
        }

        Weapon currentWeapon = _weaponController.CurrentWeapon;
        WeaponData data = currentWeapon.BaseData;

        // 4. 무기 공격력
        SetText(_weaponAttackText, data.damage.ToString());

        // 5. 연사 속도
        SetText(_fireRateText, $"{data.coolTime:F2}s");

        // 6. 반동 (탄퍼짐) 계산
        if (data is RangedWeaponData gunData)
        {
            float baseSpread = gunData.spreadAngle;
            float reduction = _player.SpreadReduction;
            float finalSpread = Mathf.Max(0, baseSpread - reduction);

            SetText(_recoilText, finalSpread.ToString("F1"));
        }
        else
        {
            SetText(_recoilText, "0");
        }
    }

    private void SetText(TMP_Text textComp, string value)
    {
        if (textComp != null) textComp.text = value;
    }
}