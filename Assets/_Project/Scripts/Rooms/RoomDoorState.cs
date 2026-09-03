namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public enum RoomDoorState // 단일 문 상태 열거형
    {
        Closed, // 연결되지 않아 통과할 수 없는 문
        Open, // 연결되어 통과 가능한 문
        Locked // 연결은 있지만 현재 잠긴 문
    }
}
