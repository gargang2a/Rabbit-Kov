// 데미지를 받을 수 있는 모든 오브젝트가 구현해야 하는 인터페이스
public interface IDamageable
{
    // 데미지 받기. amount: 받을 데미지량
    void TakeDamage(float amount);
}
