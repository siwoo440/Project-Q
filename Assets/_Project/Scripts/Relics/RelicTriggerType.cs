namespace ProjectQ.Relics // 유물 시스템 네임스페이스
{
    public enum RelicTriggerType // 조건부 유물 발동 시점 열거형
    {
        Passive, // 획득 즉시 적용되는 기존 패시브
        OnCardUsed, // 카드 사용 성공 시 발동
        OnEnemyKilled, // 적 처치 시 발동
        OnPlayerHit, // 플레이어 피격 시 발동
        OnDodge, // 회피 시작 성공 시 발동
        OnCombatStart, // 전투 시작 시 발동
        OnCombatClear // 전투 클리어 시 발동
    }
}
