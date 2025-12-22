using System.Collections.Generic;
using UnityEngine;
using System;

// [Role] 인벤토리와 별개로 탄약 수량만 관리하는 전용 매니저
public class PlayerAmmoManager : MonoBehaviour
{
    // 탄약 종류별 개수 저장 (Key: 탄약 아이템 데이터, Value: 개수)
    private Dictionary<ItemData, int> _ammoWallet = new Dictionary<ItemData, int>();

    // UI 갱신용 이벤트 (변경된 탄약종류, 현재개수)
    public event Action<ItemData, int> OnAmmoChanged;

    // 1. 탄약 획득 (ItemPickup에서 호출)
    public void AddAmmo(ItemData ammoData, int amount)
    {
        if (ammoData == null) return;

        if (_ammoWallet.ContainsKey(ammoData))
        {
            _ammoWallet[ammoData] += amount;
        }
        else
        {
            _ammoWallet.Add(ammoData, amount);
        }

        Debug.Log($"탄약 획득: {ammoData.itemName} +{amount} (총 {_ammoWallet[ammoData]}발)");

        // UI 알림
        OnAmmoChanged?.Invoke(ammoData, _ammoWallet[ammoData]);
    }

    // 2. 탄약 사용 (재장전 시 호출)
    // 성공 시 true, 부족하면 false 반환
    public bool ConsumeAmmo(ItemData ammoData, int amount)
    {
        if (ammoData == null) return false;
        if (!_ammoWallet.ContainsKey(ammoData)) return false;

        if (_ammoWallet[ammoData] >= amount)
        {
            _ammoWallet[ammoData] -= amount;
            OnAmmoChanged?.Invoke(ammoData, _ammoWallet[ammoData]);
            return true;
        }

        return false; // 탄약 부족
    }

    // 3. 현재 보유량 확인 (UI 표시용)
    public int GetAmmoCount(ItemData ammoData)
    {
        if (ammoData != null && _ammoWallet.ContainsKey(ammoData))
        {
            return _ammoWallet[ammoData];
        }
        return 0;
    }
}