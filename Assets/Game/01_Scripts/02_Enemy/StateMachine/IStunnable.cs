using System;

// [역할] 스턴 가능한 유닛 인터페이스 (Epic/Boss만 구현)
public interface IStunnable
{
    bool IsStunned { get; }           // 스턴 상태 여부
    void ApplyStun(float duration);   // 스턴 적용 (초)
    void ClearStun();                 // 스턴 해제
    event Action OnStunStart;         // 스턴 시작 이벤트
    event Action OnStunEnd;           // 스턴 종료 이벤트
}
