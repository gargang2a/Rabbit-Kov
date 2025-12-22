using UnityEngine;
using TMPro;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text _statAttackText;
    [SerializeField] private TMP_Text _weaponAttackText;
    [SerializeField] private TMP_Text _fireRateText;
    [SerializeField] private TMP_Text _recoilText;
    [SerializeField] private TMP_Text _weightText;
    [SerializeField] private TMP_Text _expText;
    [SerializeField] private TMP_Text _speedText; // ★ 연결 필수

    [Header("References")]
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

        // 스탯 공격력
        SetText(_statAttackText, _player.BaseAttack.ToString());
        // 경험치
        SetText(_expText, _player.CurrentExp.ToString());
        // 무게
        SetText(_weightText, $"{_player.CurrentWeight:F1} / {_player.MaxWeight:F0}");

        // ★ 이동속도 (여기서 갱신됨)
        SetText(_speedText, _player.MoveSpeed.ToString("F1"));

        UpdateWeaponStats();
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

        WeaponData data = _weaponController.CurrentWeapon.BaseData;
        SetText(_weaponAttackText, data.damage.ToString());
        SetText(_fireRateText, $"{data.coolTime:F2}s");

        if (data is RangedWeaponData gunData)
        {
            float finalSpread = Mathf.Max(0, gunData.spreadAngle - _player.SpreadReduction);
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