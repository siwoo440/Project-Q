namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    public sealed class PlayerProjectile : ProjectileBase // 플레이어 투사체 클래스
    {
        public override CombatFaction Faction => CombatFaction.Player; // 플레이어 투사체 진영 반환
    }
}
