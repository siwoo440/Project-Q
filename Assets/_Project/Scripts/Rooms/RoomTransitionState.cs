namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public enum RoomTransitionState // 현재 구역 이동 처리 상태 열거형
    {
        Idle, // 구역 이동 입력 대기 상태
        Moving // Door 이동 처리와 재진입 잠금 상태
    }
}
