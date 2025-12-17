using System;

/// <summary>
/// [Role] 스턴 가능한 유닛 인터페이스
/// Epic/Boss 몬스터가 구현 - Normal 몬스터는 스턴 불가
/// </summary>
public interface IStunnable
{
    /// <summary>현재 스턴 상태 여부</summary>
    bool IsStunned { get; }
    
    /// <summary>
    /// 스턴 적용 (duration초 동안)
    /// 이미 스턴 중이면 더 긴 시간으로 갱신
    /// </summary>
    void ApplyStun(float duration);
    
    /// <summary>스턴 즉시 해제</summary>
    void ClearStun();
    
    /// <summary>스턴 시작 시 발생</summary>
    event Action OnStunStart;
    
    /// <summary>스턴 종료 시 발생</summary>
    event Action OnStunEnd;
}
