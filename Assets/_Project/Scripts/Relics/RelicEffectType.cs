namespace ProjectQ.Relics // 유물 시스템 네임스페이스
{
    public enum RelicEffectType // 기본 유물 패시브 효과 유형 열거형
    {
        MaxHealthFlat, // 최대 HP 고정 증가
        MaxManaFlat, // 최대 MP 고정 증가
        BaseManaRegenFlat, // 기본 초당 MP 회복 고정 증가
        AttackDamagePercent // 카드 공격 피해 비율 증가
    }
}
