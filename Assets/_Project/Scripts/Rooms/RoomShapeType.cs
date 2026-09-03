namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public enum RoomShapeType // 실제 플레이 공간 형태 열거형
    {
        Square, // 기존 사각형 Room 형태
        LShape, // 두 방향이 직각으로 꺾이는 ㄱ자 Room 형태
        TShape, // 세 방향이 갈라지는 T자 Room 형태
        Cross, // 네 방향이 교차하는 십자 Room 형태
        Corridor, // 두 반대 방향을 잇는 좁은 복도 Room 형태
        Arena // 넓은 전투 중심 Room 형태
    }
}
