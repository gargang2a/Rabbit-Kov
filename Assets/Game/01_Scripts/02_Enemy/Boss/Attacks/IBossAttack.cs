using UnityEngine;

/// <summary>
/// [Role] 보스 공격 인터페이스
/// 모든 보스 공격 패턴이 구현해야 하는 공통 인터페이스
/// </summary>
public interface IBossAttack
{
    /// <summary>공격 이름 (디버깅/UI용)</summary>
    string AttackName { get; }
    
    /// <summary>공격 쿨다운 (초)</summary>
    float Cooldown { get; }
    
    /// <summary>공격 실행</summary>
    /// <param name="boss">보스 컨트롤러 참조</param>
    /// <param name="target">타겟 (플레이어)</param>
    void Execute(BossController boss, Transform target);
    
    /// <summary>공격 중단 (페이즈 전환 등)</summary>
    void Cancel();
    
    /// <summary>공격 실행 중 여부</summary>
    bool IsExecuting { get; }
}
