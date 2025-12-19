using UnityEngine;

// 데미지 처리 인터페이스 - 플레이어와 적 모두 구현 필요
public interface IDamageable
{
    // 상세 버전: 데미지, 타격 위치, 공격 방향, 넉백 강도
    void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection, float knockbackForce);
    
    // 중간 버전: 데미지, 타격 위치, 공격 방향 (기본 넉백)
    void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection);
    
    // 간단 버전: 데미지만 (넉백 불필요 시)
    void TakeDamage(int damage);
}
