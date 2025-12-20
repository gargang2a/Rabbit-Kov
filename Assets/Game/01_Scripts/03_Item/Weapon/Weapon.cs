using UnityEngine;
using System.Collections;

// ==========================================
// 1. 무기 동작 최상위 클래스
// ==========================================
public abstract class Weapon : MonoBehaviour
{
    protected WeaponData _baseData;

    // 내부 변수 (자식 클래스들과 공유)
    protected bool _isReady = true;

    // ★ [Fix] 자식들에서 중복 선언하지 말고 여기서 통합 관리
    // protected: 상속받은 자식 스크립트만 쓸 수 있음
    protected bool _isAttacking = false;
    protected bool _isReloading = false;

    // ★ 외부(Controller/UI)에서 읽을 수 있는 프로퍼티
    public bool IsReady => _isReady;
    public bool IsAttacking => _isAttacking;   // 추가됨
    public bool IsReloading => _isReloading;   // 추가됨
    public WeaponData BaseData => _baseData;

    // 초기화 함수
    public virtual void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        _baseData = data;
    }

    public abstract void Use();    // 공격
    public virtual void Reload() { } // 재장전

    // ★ [Fix] 무기 스왑 버그 해결의 핵심
    // 무기가 비활성화(주머니로 들어감)될 때 모든 상태를 리셋합니다.
    protected virtual void OnDisable()
    {
        _isAttacking = false;
        _isReloading = false;
        _isReady = true; // 다시 꺼낼 때 바로 쏠 수 있게

        StopAllCoroutines(); // 진행 중이던 장전/공격 코루틴 강제 종료

        // Debug.Log($"{gameObject.name} 상태 리셋 완료");
    }
}