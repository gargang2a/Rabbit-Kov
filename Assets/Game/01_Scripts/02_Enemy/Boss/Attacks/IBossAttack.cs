using UnityEngine;

// [역할] 보스 공격 인터페이스 - 모든 보스 공격 패턴이 구현
public interface IBossAttack
{
    string AttackName { get; }                         // 공격 이름
    float Cooldown { get; }                            // 쿨다운
    void Execute(BossController boss, Transform target); // 공격 실행
    void Cancel();                                     // 공격 중단
    bool IsExecuting { get; }                          // 실행 중 여부
}
