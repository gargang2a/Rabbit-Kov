using UnityEngine;
using System.Collections;

// ==========================================
// 1. 무기 동작 최상위 클래스
// ==========================================
public abstract class Weapon : MonoBehaviour
{
    protected WeaponData _baseData;

    // 내부 변수 (자식 클래스용)
    protected bool _isReady = true;

    // ★ [핵심 수정] 외부(Controller)에서 읽을 수 있는 프로퍼티 추가
    public bool IsReady => _isReady;

    // 데이터 접근용 프로퍼티 (필요 시 사용)
    public WeaponData BaseData => _baseData;

    // 초기화 함수 (발사 위치 오버라이드 포함)
    public virtual void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        _baseData = data;
    }

    public abstract void Use();    // 공격
    public virtual void Reload() { } // 재장전
}
