namespace ProjectQ.Relics // 유물 시스템 네임스페이스
{
    public enum RelicEffectType // 유물 효과 유형 열거형
    {
        MaxHealthFlat, // 최대 HP 고정 증가
        MaxManaFlat, // 최대 MP 고정 증가
        BaseManaRegenFlat, // 기본 초당 MP 회복 고정 증가
        AttackDamagePercent, // 회차 영구 카드 공격 피해 비율 증가
        RestoreManaFlat, // 조건부 MP 즉시 회복
        AddShieldFlat, // 조건부 실드 즉시 추가
        AddGoldFlat, // 조건부 골드 즉시 획득
        TemporaryAttackDamagePercent // 조건부 임시 공격 카드 피해 비율 증가
    }
}
