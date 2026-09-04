namespace ProjectQ.Bosses // 보스 시스템 네임스페이스
{
    public enum BossBattleState // 보스 전투 전체 상태 열거형
    {
        Waiting, // 보스방 진입 대기 상태
        Intro, // 보스 전투 시작 준비 상태
        Fighting, // 보스 전투 진행 상태
        Defeated, // 보스 체력 소진 상태
        Cleared // 보스방 클리어 완료 상태
    }
}
