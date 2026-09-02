namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    public interface IDamageable // 공통 피해 대상 인터페이스
    {
        CombatFaction Faction { get; } // 피해 대상 진영 반환 속성
        bool TakeDamage(DamageInfo damageInfo); // 공통 피해 적용 메서드
    }
}
