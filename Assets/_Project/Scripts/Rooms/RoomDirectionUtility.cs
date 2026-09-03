using UnityEngine; // Unity 격자 좌표 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public static class RoomDirectionUtility // 방향 반대값과 격자 좌표 변환 공통 유틸리티
    {
        public static RoomDirection Opposite(RoomDirection direction) // 지정 방향의 반대 방향 반환 메서드
        {
            switch (direction) // 지정 방향별 반대 방향 분기
            {
                case RoomDirection.Up: // 위쪽 방향 처리
                    return RoomDirection.Down; // 아래쪽 방향 반환
                case RoomDirection.Down: // 아래쪽 방향 처리
                    return RoomDirection.Up; // 위쪽 방향 반환
                case RoomDirection.Left: // 왼쪽 방향 처리
                    return RoomDirection.Right; // 오른쪽 방향 반환
                default: // 오른쪽 방향 처리
                    return RoomDirection.Left; // 왼쪽 방향 반환
            }
        }

        public static Vector2Int ToOffset(RoomDirection direction) // 방향을 격자 좌표 오프셋으로 변환하는 메서드
        {
            switch (direction) // 지정 방향별 좌표 오프셋 분기
            {
                case RoomDirection.Up: // 위쪽 방향 처리
                    return Vector2Int.up; // 위쪽 한 칸 반환
                case RoomDirection.Down: // 아래쪽 방향 처리
                    return Vector2Int.down; // 아래쪽 한 칸 반환
                case RoomDirection.Left: // 왼쪽 방향 처리
                    return Vector2Int.left; // 왼쪽 한 칸 반환
                default: // 오른쪽 방향 처리
                    return Vector2Int.right; // 오른쪽 한 칸 반환
            }
        }
    }
}
