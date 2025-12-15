using UnityEngine;

public interface IDamageable
{
    // 데미지, 타격 위치, 넉백을 위한 공격 방향을 받습니다.
    void TakeDamage(int damage, Vector3 hitPoint, Vector3 attackDirection);
}