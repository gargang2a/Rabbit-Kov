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

        if (_speedText != null)
            _speedText.text = _player.MoveSpeed.ToString("F1");
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

        // 6. ★ [핵심 수정] 무기 자체의 최종 반동 값(FinalSpread)을 가져옴
        if (currentWeapon is RangedWeapon gun)
        {
            // gun.FinalSpread는 (기본값 - 부착물 - 플레이어스탯)이 모두 계산된 값입니다.
            SetText(_recoilText, gun.FinalSpread.ToString("F1"));
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