/// <summary>
/// [Role] 적 티어 분류 - Normal, Epic, Boss
/// 티어에 따라 AI 행동, 보상, 능력치 스케일이 달라짐
/// </summary>
public enum EnemyTier
{
    /// <summary>일반 몬스터 - 기본 AI, 플레이어 감지 시 끝까지 추적</summary>
    Normal,
    
    /// <summary>에픽 몬스터 - Zone 내 순찰/감지, 제한된 추적</summary>
    Epic,
    
    /// <summary>보스 몬스터 - 페이즈 시스템, 다양한 공격 패턴</summary>
    Boss
}
