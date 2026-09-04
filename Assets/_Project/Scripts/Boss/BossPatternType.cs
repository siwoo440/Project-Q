namespace ProjectQ.Bosses // 보스 시스템 네임스페이스
{
    public enum BossPatternType // 보스 공통 테스트 공격 패턴 열거형
    {
        AimedSpread, // 플레이어 조준 확산탄 패턴
        RadialBurst, // 보스 중심 방사형 패턴
        RotatingRadial // 발사 각도 누적 회전 방사형 패턴
    }
}
