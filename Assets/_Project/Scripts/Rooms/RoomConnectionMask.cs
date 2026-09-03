using System; // 비트 플래그 열거형 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [Flags] // 상하좌우 연결 방향 복수 조합 허용
    public enum RoomConnectionMask // Room 연결 방향 비트 마스크 열거형
    {
        None = 0, // 연결 방향 없음
        Up = 1 << 0, // 위쪽 연결 방향
        Down = 1 << 1, // 아래쪽 연결 방향
        Left = 1 << 2, // 왼쪽 연결 방향
        Right = 1 << 3, // 오른쪽 연결 방향
        All = Up | Down | Left | Right // 상하좌우 전체 연결 방향
    }
}
