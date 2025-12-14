using UnityEngine;

public class DummyEnemy : EnemyBase
{
    // Dummy만의 특별한 기능이 필요하면 여기에 추가
    // 예: 체력이 닳으면 다시 회복한다거나, 절대 죽지 않는다거나.

    protected override void Die()
    {
        // 더미는 죽지 않고 체력이 초기화되게 하고 싶다면:
        // _currentHealth = _maxHealth;
        // Debug.Log("Dummy Reset!");

        // 혹은 부모의 Die(사망 처리)를 그대로 따름
        base.Die();
    }
}