using UnityEngine;
using TMPro; // ★ 이 부분이 필수입니다!

public class TotalAmmoDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("총알 개수를 표시할 텍스트 (TextMeshPro 사용)")]
    // ★ 자료형을 Text -> TMP_Text로 변경했습니다.
    [SerializeField] private TMP_Text _totalAmmoText;

    [Header("System Reference")]
    [SerializeField] private PlayerAmmoManager _ammoManager;

    [Header("Display Settings")]
    [Tooltip("화면에 표시할 탄약의 종류 (예: Data_Ammo_556)")]
    [SerializeField] private ItemData _targetAmmoData;

    private void Start()
    {
        if (_ammoManager == null)
            _ammoManager = FindObjectOfType<PlayerAmmoManager>();

        if (_ammoManager != null)
        {
            _ammoManager.OnAmmoChanged += UpdateDisplay;

            // 초기값 표시
            UpdateDisplay(_targetAmmoData, _ammoManager.GetAmmoCount(_targetAmmoData));
        }
        else
        {
            if (_totalAmmoText != null) _totalAmmoText.text = "0";
        }
    }

    private void UpdateDisplay(ItemData changedAmmoData, int newAmount)
    {
        // 탄약 데이터가 없거나, 변경된 탄약이 내가 보여줄 탄약이 아니면 무시
        if (_targetAmmoData == null || changedAmmoData != _targetAmmoData) return;

        if (_totalAmmoText != null)
        {
            _totalAmmoText.text = newAmount.ToString();
        }
    }

    private void OnDestroy()
    {
        if (_ammoManager != null)
        {
            _ammoManager.OnAmmoChanged -= UpdateDisplay;
        }
    }
}