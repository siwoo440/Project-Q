namespace ProjectQ.Run // 회차 진행 시스템 네임스페이스
{
    public enum RunPhase // 현재 회차 진행 단계 열거형
    {
        Boot, // 첫 전투 시작 전 초기화
        Combat, // 전투 진행
        Reward, // 무료 보상 선택
        Shop, // 골드 상점 이용
        GameOver // 현재 전투 실패
    }
}
