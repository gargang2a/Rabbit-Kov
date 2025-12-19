// [역할] 적 티어 분류 - 티어에 따라 AI, 보상, 능력치가 달라짐
public enum EnemyTier
{
    Normal, // 일반 - 기본 AI, 플레이어 감지 시 끝까지 추적
    Epic,   // 에픽 - Zone 내 순찰/감지, 제한된 추적
    Boss    // 보스 - 페이즈 시스템, 다양한 공격 패턴
}
